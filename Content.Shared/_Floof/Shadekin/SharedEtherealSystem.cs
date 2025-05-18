using Content.Shared.Physics;
using Robust.Shared.Physics;
using System.Linq;
using Robust.Shared.Physics.Systems;
using Content.Shared.Interaction.Events;
using Robust.Shared.Timing;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Mobs;
using Robust.Shared.Configuration;
using Content.Shared.Tag;
using Content.Shared.Standing;
using Content.Shared.Damage.Systems;


namespace Content.Shared._Floof.Shadekin;

public abstract class SharedEtherealSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly StandingStateSystem _standingStateSystem = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EtherealComponent, MapInitEvent>(OnStartup);
        SubscribeLocalEvent<EtherealComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EtherealComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<EtherealComponent, BeforeThrowEvent>(OnBeforeThrow);
        SubscribeLocalEvent<EtherealComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<EtherealComponent, ShotAttemptedEvent>(OnShootAttempt);
        SubscribeLocalEvent<EtherealComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<EtherealComponent, DownAttemptEvent>(DownAttemptEvent);
    }

    public virtual void OnStartup(EntityUid uid, EtherealComponent component, MapInitEvent args)
    {
        if (!TryComp<FixturesComponent>(uid, out var fixtures))
            return;

        if (TryComp<StandingStateComponent>(uid, out var standingstate))
        {
            _standingStateSystem.Stand(uid, standingstate);
        }

        var fixture = fixtures.Fixtures.First();

        component.OldMobMask = fixture.Value.CollisionMask;
        component.OldMobLayer = fixture.Value.CollisionLayer;

        _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, (int) CollisionGroup.GhostImpassable, fixtures);
        _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, 0, fixtures);

        if (_tag.RemoveTag(uid, "DoorBumpOpener"))
            component.HasDoorBumpTag = true;
    }

    public virtual void OnShutdown(EntityUid uid, EtherealComponent component, ComponentShutdown args)
    {
        if (!TryComp<FixturesComponent>(uid, out var fixtures))
            return;

        var fixture = fixtures.Fixtures.First();

        _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, component.OldMobMask, fixtures);
        _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, component.OldMobLayer, fixtures);

        if (component.HasDoorBumpTag)
            _tag.AddTag(uid, "DoorBumpOpener");
    }

    private void OnMobStateChanged(EntityUid uid, EtherealComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Critical
            || args.NewMobState == MobState.Dead)
        {
            SpawnAtPosition("ShadekinShadow", Transform(uid).Coordinates);
            RemComp(uid, component);
        }
    }

    private void OnShootAttempt(Entity<EtherealComponent> ent, ref ShotAttemptedEvent args)
    {
        args.Cancel();
    }

    private void OnAttackAttempt(EntityUid uid, EtherealComponent component, AttackAttemptEvent args)
    {
        if (HasComp<EtherealComponent>(args.Target))
            return;

        args.Cancel();
    }

    private void OnBeforeThrow(Entity<EtherealComponent> ent, ref BeforeThrowEvent args)
    {
        var thrownItem = args.ItemUid;

        // Raise an AttemptPacifiedThrow event and rely on other systems to check
        // whether the candidate item is OK to throw:
        var ev = new AttemptPacifiedThrowEvent(thrownItem, ent);
        RaiseLocalEvent(thrownItem, ref ev);
        if (!ev.Cancelled)
            return;

        args.Cancelled = true;
    }

    private void OnInteractionAttempt(EntityUid uid, EtherealComponent component, ref InteractionAttemptEvent args)
    {
        if (HasComp<EtherealComponent>(args.Target))
            return;

        args.Cancelled = true;
        if (_gameTiming.InPrediction)
            return;
    }

    private void DownAttemptEvent(EntityUid uid, EtherealComponent component, DownAttemptEvent args)
    {
        args.Cancel();
    }
}
