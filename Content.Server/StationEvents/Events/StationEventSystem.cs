using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Server.Announcements.Systems;
using Robust.Shared.Player;
using Content.Server.Station.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Timing;


namespace Content.Server.StationEvents.Events;

/// <summary>
///     An abstract entity system inherited by all station events for their behavior.
/// </summary>
public abstract class StationEventSystem<T> : GameRuleSystem<T> where T : IComponent
{
    [Dependency] protected readonly IAdminLogManager AdminLogManager = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly ChatSystem ChatSystem = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly StationSystem StationSystem = default!;
    [Dependency] private readonly AnnouncerSystem _announcer = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!; // Floof

    protected ISawmill Sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        Sawmill = Logger.GetSawmill("stationevents");
    }

    /// <inheritdoc/>
    protected override void Added(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;


        AdminLogManager.Add(LogType.EventAnnounced, $"Event added / announced: {ToPrettyString(uid)}");

        stationEvent.StartTime = Timing.CurTime + stationEvent.StartDelay;
    }

    /// <inheritdoc/>
    protected override void Started(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        AdminLogManager.Add(LogType.EventStarted, LogImpact.High, $"Event started: {ToPrettyString(uid)}");

        if (stationEvent.StartAnnouncement)
        {
            _announcer.SendAnnouncement(
                _announcer.GetAnnouncementId(args.RuleId),
                Filter.Broadcast(),
                _announcer.GetEventLocaleString(_announcer.GetAnnouncementId(args.RuleId)),
                colorOverride: Color.Gold
            );
        }

        if (stationEvent.Duration != null)
        {
            var duration = stationEvent.MaxDuration == null
                ? stationEvent.Duration
                : TimeSpan.FromSeconds(RobustRandom.NextDouble(stationEvent.Duration.Value.TotalSeconds,
                    stationEvent.MaxDuration.Value.TotalSeconds));
            stationEvent.EndTime = Timing.CurTime + duration;
        }
    }

    /// <inheritdoc/>
    protected override void Ended(EntityUid uid, T component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        AdminLogManager.Add(LogType.EventStopped, $"Event ended: {ToPrettyString(uid)}");

        if (stationEvent.EndAnnouncement)
        {
            _announcer.SendAnnouncement(
                _announcer.GetAnnouncementId(args.RuleId, true),
                Filter.Broadcast(),
                _announcer.GetEventLocaleString(_announcer.GetAnnouncementId(args.RuleId, true)),
                colorOverride: Color.Gold);
        }
    }

    /// <summary>
    ///     Called every tick when this event is running.
    ///     Events are responsible for their own lifetime, so this handles starting and ending after time.
    /// </summary>
    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StationEventComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var stationEvent, out var ruleData))
        {
            if (!GameTicker.IsGameRuleAdded(uid, ruleData))
                continue;

            if (!GameTicker.IsGameRuleActive(uid, ruleData) && Timing.CurTime >= stationEvent.StartTime)
            {
                GameTicker.StartGameRule(uid, ruleData);
            }
            else if (stationEvent.EndTime != null && Timing.CurTime >= stationEvent.EndTime && GameTicker.IsGameRuleActive(uid, ruleData))
            {
                GameTicker.EndGameRule(uid, ruleData);
            }
        }
    }

    #region Helper Functions

    protected void ForceEndSelf(EntityUid uid, GameRuleComponent? component = null)
    {
        GameTicker.EndGameRule(uid, component);
    }

    // Floof section - a function to delay the appearance of spawned mobs

    /// <summary>
    ///     A helper function to spawn an entity after a delay while simulatenously showing a popup and/or playing a sound.
    /// </summary>
    /// <param name="protoId">The prototype to spawn.</param>
    /// <param name="coordinates">The coordinates to spawn at.</param>
    /// <param name="delay">A vector representing the minimum and maximum delay (in seconds). If null, no delay is applied.</param>
    /// <param name="popup">The popup to show when the delay is started, if any..</param>
    /// <param name="sound">The sound to play when the delay is started, if any.</param>
    protected Task<EntityUid> DelayedSpawn(EntProtoId? protoId, EntityCoordinates coordinates, Vector2? delay, LocId? popup, SoundSpecifier? sound)
    {
        // Show the popup and play the sound, if any
        if (popup is {} popupLoc)
            _popups.PopupCoordinates(Loc.GetString(popupLoc), coordinates, PopupType.MediumCaution);
        if (sound is {} soundSpecifier)
            Audio.PlayStatic(soundSpecifier, Filter.Pvs(coordinates), coordinates, true, soundSpecifier.Params);

        // If no delay is specified, just spawn the entity on the current thread and return
        if (delay == null)
            return Task.FromResult(Spawn(protoId, coordinates));

        // Otherwise, spawn a new timer that will spawn the entity after the delay, and set the result of this task.
        var waitTime = (int) RobustRandom.NextFloat(delay.Value.X, delay.Value.Y) * 1000;
        var tcs = new TaskCompletionSource<EntityUid>();

        Timer.Spawn(waitTime, () =>
        {
            // Let's hope this will prevent the timer from being carried across rounds.
            if (!coordinates.IsValid(EntityManager))
                return;

            try
            {
                var spawned = Spawn(protoId, coordinates);
                tcs.SetResult(spawned);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        });

        return tcs.Task;
    }

    /// <summary>
    ///     A helper function to delay by a certain random time, then show a popup and play a sound (optionally),
    ///     and then delay again and finally spawn the mob. Used to make the appearence of entities less sudden and more random at the same time.<br/><br/>
    ///
    ///     The resulting delay between the call of this function and the actual spawn is going to be random(initialDelay) + random(afterPopupDelay),
    ///     with the popup and sound being played after the first of them.
    /// </summary>
    /// <see cref="DelayedSpawn(EntProtoId?, EntityCoordinates, Vector2?, LocId?, SoundSpecifier?)"/>
    protected void DelayedSpawn(
        EntProtoId? protoId,
        EntityCoordinates coordinates,
        Vector2 initialDelay,
        Vector2? afterPopupDelay,
        LocId? popup,
        SoundSpecifier? sound)
    {
        // I don't want to make this return the actual uid because it's almost never actually gonna get used, and concurrency is pain.
        // If you want to get the spawned entity uid (possibly many seconds or minutes after this function has been invoked),
        // just set up your own function similar to this one.
        var initialWait = (int) RobustRandom.NextFloat(initialDelay.X, initialDelay.Y) * 1000;

        Timer.Spawn(initialWait, () =>
        {
            DelayedSpawn(protoId, coordinates, afterPopupDelay, popup, sound);
        });
    }

    // Floof section end

    #endregion
}
