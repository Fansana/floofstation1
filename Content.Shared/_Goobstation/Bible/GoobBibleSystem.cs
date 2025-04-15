using Content.Shared._Goobstation.Devil;
using Content.Shared._Goobstation.Exorcism;
using Content.Shared._Goobstation.Religion;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._Goobstation.Bible;

public sealed partial class GoobBibleSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public void TryDoSmite(EntityUid uid, BibleComponent component, AfterInteractEvent args, UseDelayComponent useDelay)
    {
        if (!HasComp<WeakToHolyComponent>(args.Target) || !HasComp<BibleUserComponent>(args.User))
            return;

        if (!_mobStateSystem.IsIncapacitated(args.Target.Value))
        {
            _popupSystem.PopupEntity(Loc.GetString("devil-component-bible-sizzle", ("target", args.Target.Value)),
                args.Target.Value,
                PopupType.LargeCaution);
            _audio.PlayPvs(component.SizzleSoundPath, args.Target.Value);

            _damageableSystem.TryChangeDamage(args.Target, component.SmiteDamage, true, origin: uid);
            _stun.TryParalyze(args.Target.Value, component.SmiteStunDuration, false);
            _delay.TryResetDelay((uid, useDelay));
        }
        else if (HasComp<DevilComponent>(args.Target) && HasComp<BibleUserComponent>(args.User))
        {
            var doAfterArgs = new DoAfterArgs(
                EntityManager,
                args.User,
                10f,
                new ExorcismDoAfterEvent(),
                eventTarget: args.Target.Value,
                target: args.Target.Value)
            {
                BreakOnMove = true,
                NeedHand = true,
                BlockDuplicate = true,
                //BreakOnDropItem = true, TODO decide if we need this
            };

            _doAfter.TryStartDoAfter(doAfterArgs);
            _popupSystem.PopupEntity(
                Loc.GetString("devil-banish-begin", ("target", args.Target.Value), ("user", args.User)),
                args.Target.Value,
                PopupType.LargeCaution);
        }
    }
}
