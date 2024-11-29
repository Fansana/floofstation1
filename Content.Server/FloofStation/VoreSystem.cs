using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Utility;
using Robust.Shared.Audio.Systems;
using Content.Server.Body.Components;
using Content.Server.Consent;
using Content.Shared.Mobs.Components;
using Content.Shared.Examine;
using Content.Server.Atmos.Components;
using Content.Server.Temperature.Components;
using Content.Server.Cuffs;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Interaction.Events;

namespace Content.Server.FloofStation;

public sealed class VoreSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ConsentSystem _consent = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VoreComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<VoreComponent, GetVerbsEvent<InnateVerb>>(AddVerbs);
        SubscribeLocalEvent<VoreComponent, BeingGibbedEvent>(OnGibContents);
        SubscribeLocalEvent<VoreComponent, ExaminedEvent>((uid, _, args) => OnExamine(uid, args));

        SubscribeLocalEvent<VoredComponent, DropAttemptEvent>(CheckAct);
        SubscribeLocalEvent<VoredComponent, PickupAttemptEvent>(CheckAct);
        SubscribeLocalEvent<VoredComponent, AttackAttemptEvent>(CheckAct);
        SubscribeLocalEvent<VoredComponent, UseAttemptEvent>(CheckAct);
        SubscribeLocalEvent<VoredComponent, InteractionAttemptEvent>(CheckAct);
    }

    private void OnInit(EntityUid uid, VoreComponent component, MapInitEvent args)
    {
        component.Stomach = _containerSystem.EnsureContainer<Container>(uid, "stomach");
    }

    private void AddVerbs(EntityUid uid, VoreComponent component, GetVerbsEvent<InnateVerb> args)
    {
        DevourVerb(uid, component, args);
        VoreVerb(uid, component, args);
    }

    private void DevourVerb(EntityUid uid, VoreComponent component, GetVerbsEvent<InnateVerb> args)
    {
        if (!args.CanInteract
            || !args.CanAccess
            || args.User == args.Target
            || !HasComp<MobStateComponent>(args.Target)
            || !_consent.HasConsent(args.Target, "Vore"))
            return;

        InnateVerb verbDevour = new()
        {
            Act = () => Devour(uid, args.Target, component),
            Text = Loc.GetString("vore-devour"),
            Category = VerbCategory.Vore,
            Icon = new SpriteSpecifier.Rsi(new ResPath("Interface/Actions/devour.rsi"), "icon-on"),
            Priority = -1
        };
        args.Verbs.Add(verbDevour);
    }

    private void VoreVerb(EntityUid uid, VoreComponent component, GetVerbsEvent<InnateVerb> args)
    {
        if (args.User != args.Target)
            return;

        foreach (var prey in component.Stomach.ContainedEntities)
        {
            InnateVerb verbRelease = new()
            {
                Act = () => Release(uid, prey, component),
                Text = Loc.GetString("vore-release", ("entity", prey)),
                Category = VerbCategory.Vore,
                Priority = 2
            };
            args.Verbs.Add(verbRelease);

            if (_consent.HasConsent(prey, "Digestion"))
            {
                InnateVerb verbDigest = new()
                {
                    Act = () => Release(uid, prey, component),
                    Text = Loc.GetString("vore-digest", ("entity", prey)),
                    Category = VerbCategory.Vore,
                    Priority = 1,
                    ConfirmationPopup = true
                };
                args.Verbs.Add(verbDigest);
            }
        }
    }

    public void Devour(EntityUid uid, EntityUid target, VoreComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var vored = EnsureComp<VoredComponent>(target);
        vored.Pred = uid;
        EnsureComp<PressureImmunityComponent>(target);
        EnsureComp<RespiratorImmuneComponent>(target);
        if (TryComp<TemperatureComponent>(target, out var temp))
            temp.AtmosTemperatureTransferEfficiency = 0;

        _containerSystem.Insert(target, component.Stomach);
        _audioSystem.PlayPvs(component.SoundDevour, uid);
    }

    public void Release(EntityUid uid, EntityUid target, VoreComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        RemComp<VoredComponent>(target);
        RemComp<PressureImmunityComponent>(target);
        RemComp<RespiratorImmuneComponent>(target);
        if (TryComp<TemperatureComponent>(target, out var temp))
            temp.AtmosTemperatureTransferEfficiency = 0.1f;

        _containerSystem.TryRemoveFromContainer(target, true);
        _audioSystem.PlayPvs(component.SoundDevour, uid);
    }

    public void Digest(EntityUid uid, VoredComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        // TODO Add digestion noises and startup message, then need to do an update.
    }

    private void OnExamine(EntityUid uid, ExaminedEvent args)
    {
        if (!_containerSystem.TryGetContainer(uid, "stomach", out var stomach)
            || stomach.ContainedEntities.Count < 1)
            return;

        args.PushMarkup(Loc.GetString("vore-examine", ("count", stomach.ContainedEntities.Count)), -1);
    }

    private void CheckAct(EntityUid uid, VoredComponent component, CancellableEntityEventArgs args)
    {
        args.Cancel();
    }

    private void OnGibContents(EntityUid uid, VoreComponent component, ref BeingGibbedEvent args)
    {
        foreach (var prey in component.Stomach.ContainedEntities)
        {
            Release(uid, prey, component);
        }
        // _containerSystem.EmptyContainer(component.Stomach);
    }
}
