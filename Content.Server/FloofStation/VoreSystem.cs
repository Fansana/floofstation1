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
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Damage;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Server.Chat.Managers;
using Content.Server.DoAfter;
using Content.Shared.Popups;
using Robust.Server.Player;
using Content.Shared.Mobs.Systems;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.FloofStation;
using Robust.Shared.Random;
using Content.Shared.Inventory;
using Robust.Shared.Physics.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Shared.PowerCell.Components;
using System.Linq;
using Content.Shared.Forensics;
using Content.Server.Forensics;
using Content.Shared.Contests;
using Content.Shared.Standing;
using Content.Server.Power.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Hands.EntitySystems;
using Content.Server.Carrying;

namespace Content.Server.FloofStation;

public sealed class VoreSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ConsentSystem _consent = default!;
    [Dependency] private readonly BlindableSystem _blindableSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly ContestsSystem _contests = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly FoodSystem _food = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly CarryingSystem _carrying = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoreComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<VoreComponent, GetVerbsEvent<InnateVerb>>(AddVerbs);
        SubscribeLocalEvent<VoreComponent, BeingGibbedEvent>(OnGibContents);
        SubscribeLocalEvent<VoreComponent, ExaminedEvent>((uid, _, args) => OnExamine(uid, args));
        SubscribeLocalEvent<VoreComponent, VoreDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<VoredComponent, EntGotRemovedFromContainerMessage>(OnRelease);
        SubscribeLocalEvent<VoredComponent, CanSeeAttemptEvent>(OnSeeAttempt);
        SubscribeLocalEvent<VoredComponent, InteractionAttemptEvent>(CheckInteraction);
    }

    private void OnInit(EntityUid uid, VoreComponent component, MapInitEvent args)
    {
        component.Stomach = _containerSystem.EnsureContainer<Container>(uid, "stomach");
    }

    private void AddVerbs(EntityUid uid, VoreComponent component, GetVerbsEvent<InnateVerb> args)
    {
        DevourVerb(uid, component, args);
        InsertSelfVerb(uid, args);
        VoreVerb(uid, component, args);
    }

    private void DevourVerb(EntityUid uid, VoreComponent component, GetVerbsEvent<InnateVerb> args)
    {
        if (!args.CanInteract
            || !args.CanAccess
            || args.User == args.Target
            || !TryComp<VoreComponent>(args.User, out var voreuser)
            || !voreuser.CanVore
            || !TryComp<VoreComponent>(args.Target, out var voretarget)
            || !voretarget.CanBeVored
            || !_consent.HasConsent(args.User, "VorePred")
            || !_consent.HasConsent(args.Target, "Vore"))
            return;

        InnateVerb verbDevour = new()
        {
            Act = () => TryDevour(args.User, args.Target, component, false),
            Text = Loc.GetString("vore-devour"),
            Category = VerbCategory.Interaction,
            Icon = new SpriteSpecifier.Rsi(new ResPath("Interface/Actions/devour.rsi"), "icon-on"),
            Priority = -1
        };
        args.Verbs.Add(verbDevour);
    }

    private void InsertSelfVerb(EntityUid uid, GetVerbsEvent<InnateVerb> args)
    {
        if (!args.CanInteract
            || !args.CanAccess
            || args.User == args.Target
            || !TryComp<VoreComponent>(args.User, out var voreuser)
            || !voreuser.CanBeVored
            || !TryComp<VoreComponent>(args.Target, out var voretarget)
            || !voretarget.CanVore
            || !_consent.HasConsent(args.Target, "VorePred")
            || !_consent.HasConsent(args.User, "Vore"))
            return;

        InnateVerb verbInsert = new()
        {
            Act = () => TryDevour(args.Target, args.User, voretarget, true),
            Text = Loc.GetString("action-name-insert-self"),
            Category = VerbCategory.Interaction,
            Icon = new SpriteSpecifier.Rsi(new ResPath("Interface/Actions/devour.rsi"), "icon"),
            Priority = -1
        };
        args.Verbs.Add(verbInsert);
    }

    private void VoreVerb(EntityUid uid, VoreComponent component, GetVerbsEvent<InnateVerb> args)
    {
        if (args.User != args.Target)
            return;

        foreach (var prey in component.Stomach.ContainedEntities)
        {
            InnateVerb verbRelease = new()
            {
                Act = () => _containerSystem.TryRemoveFromContainer(prey, true),
                Text = Loc.GetString("vore-release", ("entity", prey)),
                Category = VerbCategory.Vore,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Priority = 2
            };
            args.Verbs.Add(verbRelease);

            if (!TryComp<VoredComponent>(prey, out var vored))
                return;

            if (_consent.HasConsent(prey, "Digestion")
                && HasComp<DamageableComponent>(args.Target)
                && !vored.Digesting)
            {
                InnateVerb verbDigest = new()
                {
                    Act = () => Digest(prey),
                    Text = Loc.GetString("vore-digest", ("entity", prey)),
                    Category = VerbCategory.Vore,
                    Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png")),
                    Priority = 1,
                    ConfirmationPopup = true
                };
                args.Verbs.Add(verbDigest);
            }
            else if (vored.Digesting)
            {
                InnateVerb verbStopDigest = new()
                {
                    Act = () => StopDigest(prey),
                    Text = Loc.GetString("vore-stop-digest", ("entity", prey)),
                    Category = VerbCategory.Vore,
                    Priority = 1,
                };
                args.Verbs.Add(verbStopDigest);
            }
        }
    }

    public void TryDevour(EntityUid uid, EntityUid target, VoreComponent? component = null, bool isInsertion = false)
    {
        if (!Resolve(uid, ref component))
            return;

        if (_food.IsMouthBlocked(uid, uid))
            return;

        if (isInsertion) {
            _popups.PopupEntity(Loc.GetString("vore-attempt-insert", ("entity", uid), ("prey", target)), uid, target, PopupType.MediumCaution);
            _popups.PopupEntity(Loc.GetString("vore-attempt-insert", ("entity", uid), ("prey", target)), target, uid, PopupType.MediumCaution);
        } else {
            _popups.PopupEntity(Loc.GetString("vore-attempt-devour", ("entity", uid), ("prey", target)), uid, target, PopupType.MediumCaution);
            _popups.PopupEntity(Loc.GetString("vore-attempt-devour", ("entity", uid), ("prey", target)), target, uid, PopupType.MediumCaution);
        }

        if (!TryComp<PhysicsComponent>(uid, out var predPhysics)
            || !TryComp<PhysicsComponent>(target, out var preyPhysics))
            return;

        var length = TimeSpan.FromSeconds(component.Delay
                        * _contests.MassContest(preyPhysics, predPhysics, false, 4f) // Big things are harder to fit in small things
                        * _contests.StaminaContest(isInsertion?target:uid, isInsertion?uid:target) // The person doing the action having higher stamina makes it easier
                        * (_standingState.IsDown(isInsertion?uid:target) ? 0.5f : 1)); // If the person having the action done to them is on the ground it's easier

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, length, new VoreDoAfterEvent(), uid, target: target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            RequireCanInteract = true
        });
    }

    private void OnDoAfter(EntityUid uid, VoreComponent component, VoreDoAfterEvent args)
    {
        if (component is null)
            return;

        if (args.Target is null
            || args.Cancelled)
            return;

        Devour(uid, args.Target.Value, component);
    }

    public void Devour(EntityUid uid, EntityUid target, VoreComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var vored = EnsureComp<VoredComponent>(target);
        vored.Pred = uid;
        EnsureComp<PressureImmunityComponent>(target);
        EnsureComp<RespiratorImmuneComponent>(target);
        _blindableSystem.UpdateIsBlind(target);
        if (TryComp<TemperatureComponent>(target, out var temp))
            temp.AtmosTemperatureTransferEfficiency = 0;

        _carrying.DropCarried(uid, target);
        _carrying.DropCarried(target, uid);

        _containerSystem.Insert(target, component.Stomach);

        if (_playerManager.TryGetSessionByEntity(target, out var sessionprey)
            || sessionprey is not null)
            _audioSystem.PlayEntity(component.SoundDevour, sessionprey, uid);

        if (_playerManager.TryGetSessionByEntity(uid, out var sessionpred)
            || sessionpred is not null)
        {
            _audioSystem.PlayEntity(component.SoundDevour, sessionpred, uid);
            // var message = Loc.GetString("", ("entity", uid));
            // _chatManager.ChatMessageToOne(
            //     ChatChannel.Emotes,
            //     message,
            //     message,
            //     EntityUid.Invalid,
            //     false,
            //     sessionprey.Channel);
        }

        _popups.PopupEntity(Loc.GetString("vore-devoured", ("entity", uid), ("prey", target)), target, target, PopupType.SmallCaution);
        _popups.PopupEntity(Loc.GetString("vore-devoured", ("entity", uid), ("prey", target)), target, uid, PopupType.SmallCaution);

        _adminLog.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(uid)} vored {ToPrettyString(target)}");
    }

    private void OnRelease(EntityUid uid, VoredComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (!TryComp<VoreComponent>(component.Pred, out var predvore)
            || predvore.Stomach != args.Container)
            return;

        _transform.AttachToGridOrMap(uid);

        RemComp<VoredComponent>(uid);
        RemComp<PressureImmunityComponent>(uid);
        RemComp<RespiratorImmuneComponent>(uid);
        _blindableSystem.UpdateIsBlind(uid);
        if (TryComp<TemperatureComponent>(uid, out var temp))
            temp.AtmosTemperatureTransferEfficiency = 0.1f;

        if (_playerManager.TryGetSessionByEntity(args.Container.Owner, out var sessionpred)
            || sessionpred is not null)
            _audioSystem.PlayEntity(component.SoundRelease, sessionpred, uid);

        if (_playerManager.TryGetSessionByEntity(uid, out var sessionprey)
            || sessionprey is not null)
            _audioSystem.PlayEntity(component.SoundRelease, sessionprey, uid);

        _popups.PopupEntity(Loc.GetString("vore-released", ("entity", uid), ("pred", args.Container.Owner)), uid, args.Container.Owner, PopupType.Medium);
        _popups.PopupEntity(Loc.GetString("vore-released", ("entity", uid), ("pred", args.Container.Owner)), uid, uid, PopupType.Medium);

        _adminLog.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(uid)} got released from {ToPrettyString(args.Container.Owner)} belly");
    }

    public void Digest(EntityUid uid, VoredComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _adminLog.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(component.Pred)} started digesting {ToPrettyString(uid)}");

        component.Digesting = true;

        _popups.PopupEntity(Loc.GetString("vore-digest-start", ("entity", component.Pred)), component.Pred, component.Pred, PopupType.LargeCaution);
        if (_playerManager.TryGetSessionByEntity(component.Pred, out var sessionpred)
            || sessionpred is not null)
        {
            var message = Loc.GetString("vore-digest-start-chat", ("entity", component.Pred));
            _chatManager.ChatMessageToOne(
                ChatChannel.Emotes,
                message,
                message,
                EntityUid.Invalid,
                false,
                sessionpred.Channel);
        }

        _popups.PopupEntity(Loc.GetString("vore-digest-start", ("entity", component.Pred)), component.Pred, uid, PopupType.LargeCaution);
        if (_playerManager.TryGetSessionByEntity(uid, out var sessionprey)
            || sessionprey is not null)
        {
            var message = Loc.GetString("vore-digest-start-chat", ("entity", component.Pred));
            _chatManager.ChatMessageToOne(
                ChatChannel.Emotes,
                message,
                message,
                EntityUid.Invalid,
                false,
                sessionprey.Channel);
        }
    }

    public void StopDigest(EntityUid uid, VoredComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _adminLog.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(component.Pred)} stopped digesting {ToPrettyString(uid)}");

        component.Digesting = false;

        _popups.PopupEntity(Loc.GetString("vore-digest-stop", ("entity", component.Pred)), component.Pred, component.Pred, PopupType.Large);
        if (_playerManager.TryGetSessionByEntity(component.Pred, out var sessionpred)
            || sessionpred is not null)
        {
            var message = Loc.GetString("vore-digest-stop", ("entity", component.Pred));
            _chatManager.ChatMessageToOne(
                ChatChannel.Emotes,
                message,
                message,
                EntityUid.Invalid,
                false,
                sessionpred.Channel);
        }

        _popups.PopupEntity(Loc.GetString("vore-digest-stop", ("entity", component.Pred)), component.Pred, uid, PopupType.Large);
        if (_playerManager.TryGetSessionByEntity(uid, out var sessionprey)
            || sessionprey is not null)
        {
            var message = Loc.GetString("vore-digest-stop", ("entity", component.Pred));
            _chatManager.ChatMessageToOne(
                ChatChannel.Emotes,
                message,
                message,
                EntityUid.Invalid,
                false,
                sessionprey.Channel);
        }
    }

    private void FullyDigest(EntityUid uid, EntityUid prey)
    {
        _adminLog.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(uid)} fully digested {ToPrettyString(prey)}");

        var digestedmessage = _random.Next(1, 8);

        if (_playerManager.TryGetSessionByEntity(uid, out var sessionpred)
            || sessionpred is not null)
        {
            var message = Loc.GetString("vore-digested-owner-" + digestedmessage, ("entity", prey));
            _chatManager.ChatMessageToOne(
                ChatChannel.Emotes,
                message,
                message,
                EntityUid.Invalid,
                false,
                sessionpred.Channel);
        }

        if (_playerManager.TryGetSessionByEntity(prey, out var sessionprey)
            || sessionprey is not null)
        {
            var message = Loc.GetString("vore-digested-prey-" + digestedmessage, ("entity", uid));
            _chatManager.ChatMessageToOne(
                ChatChannel.Emotes,
                message,
                message,
                EntityUid.Invalid,
                false,
                sessionprey.Channel);
        }

        if (TryComp<InventoryComponent>(prey, out var inventoryComponent) && _inventorySystem.TryGetSlots(uid, out var slots))
            foreach (var slot in slots)
            {
                if (_inventorySystem.TryGetSlotEntity(prey, slot.Name, out var item, inventoryComponent))
                {
                    if (TryComp<DnaComponent>(uid, out var dna))
                    {
                        var partComp = EnsureComp<ForensicsComponent>(item.Value);
                        partComp.DNAs.Add(dna.DNA);
                        Dirty(item.Value, partComp);
                    }
                    _transform.AttachToGridOrMap(item.Value);
                }
            }

        if (TryComp<VoreComponent>(prey, out var preyvore))
            _containerSystem.EmptyContainer(preyvore.Stomach);

        QueueDel(prey);
    }

    private void OnExamine(EntityUid uid, ExaminedEvent args)
    {
        if (!(_consent.HasConsent(args.Examiner, "Vore") || _consent.HasConsent(args.Examiner, "VorePred")))
            return;

        if (!_containerSystem.TryGetContainer(uid, "stomach", out var stomach)
            || stomach.ContainedEntities.Count < 1)
            return;

        args.PushMarkup(Loc.GetString("vore-examine", ("count", stomach.ContainedEntities.Count)), -1);
    }

    private void OnSeeAttempt(EntityUid uid, VoredComponent component, CanSeeAttemptEvent args)
    {
        if (component.LifeStage <= ComponentLifeStage.Running)
            args.Cancel();
    }

    private void OnGibContents(EntityUid uid, VoreComponent component, ref BeingGibbedEvent args)
    {
        _containerSystem.EmptyContainer(component.Stomach);
    }

    private void CheckInteraction(EntityUid uid, VoredComponent component, InteractionAttemptEvent args)
    {
        if (component.Pred != args.Target)
            return;

        args.Cancel();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VoredComponent>();
        while (query.MoveNext(out var uid, out var vored))
        {
            if (!vored.Digesting)
                continue;

            vored.Accumulator += frameTime;

            if (vored.Accumulator <= 5)
                continue;

            vored.Accumulator -= 1;

            if (!_consent.HasConsent(uid, "Digestion"))
            {
                StopDigest(uid, vored);
                continue;
            }

            if (_mobState.IsDead(uid))
            {
                FullyDigest(vored.Pred, uid);
                continue;
            }
            else
            {
                DamageSpecifier damage = new();
                damage.DamageDict.Add("Caustic", 1);
                _damageable.TryChangeDamage(uid, damage, true, false);

                // Give 1 Hunger per 1 Caustic Damage.
                if (TryComp<HungerComponent>(vored.Pred, out var hunger))
                    _hunger.ModifyHunger(vored.Pred, 1, hunger);

                // Give 2 Power per 1 Caustic Damage.
                if (TryComp<BatteryComponent>(vored.Pred, out var internalbattery))
                    _battery.SetCharge(vored.Pred, internalbattery.CurrentCharge + 2, internalbattery);

                // Give 2 Power per 1 Caustic Damage.
                if (TryComp<PowerCellSlotComponent>(vored.Pred, out var batterySlot)
                    && _containerSystem.TryGetContainer(vored.Pred, batterySlot.CellSlotId, out var container)
                    && container.ContainedEntities.Count > 0)
                {
                    var battery = container.ContainedEntities.First();
                    if (TryComp<BatteryComponent>(battery, out var batterycomp))
                        _battery.SetCharge(battery, batterycomp.CurrentCharge + 2, batterycomp);
                }
            }
        }
    }
}
