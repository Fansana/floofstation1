using Content.Shared.Abilities.Psionics;
using Content.Shared.Actions.Events;
using Content.Shared.Floofstation.Hypno;
using Content.Shared.Popups;
using Content.Server.DoAfter;
using Content.Shared.DoAfter;
using Robust.Shared.Utility;
using Content.Shared.Verbs;
using Content.Shared.Examine;
using Content.Shared.Mood;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Robust.Server.Player;
using Content.Shared.Database;
using Content.Shared.Administration.Logs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Psionics;
using Content.Shared.Tag;
using Content.Shared.Implants;


namespace Content.Server.Abilities.Psionics
{
    public sealed class PsionicHypnoSystem : EntitySystem
    {
        [Dependency] private readonly SharedPsionicAbilitiesSystem _psionics = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popups = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly TagSystem _tag = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PsionicHypnoComponent, HypnoPowerActionEvent>(OnPowerUsed);
            SubscribeLocalEvent<PsionicHypnoComponent, GetVerbsEvent<InnateVerb>>(ReleaseSubjectVerb);
            SubscribeLocalEvent<HypnotizedComponent, DispelledEvent>(OnDispelledHypnotized);
            SubscribeLocalEvent<PsionicHypnoComponent, DispelledEvent>(OnDispelled);
            SubscribeLocalEvent<PsionicHypnoComponent, PsionicHypnosisDoAfterEvent>(OnDoAfter);
            SubscribeLocalEvent<HypnotizedComponent, OnMindbreakEvent>(OnMindbreak);
            SubscribeLocalEvent<HypnotizedComponent, ImplantImplantedEvent>(OnMindShield);
            SubscribeLocalEvent<HypnotizedComponent, ExaminedEvent>((uid, _, args) => OnExamine(uid, args));
        }

        private void OnPowerUsed(EntityUid uid, PsionicHypnoComponent component, HypnoPowerActionEvent args)
        {
            if (!_psionics.OnAttemptPowerUse(args.Performer, "hypno")
                || !TryComp<MobStateComponent>(args.Target, out var mob)
                || _mobState.IsDead(args.Target, mob)
                || _mobState.IsCritical(args.Target, mob))
                return;

            if (component.Subjects >= component.MaxSubjects)
            {
                _popups.PopupEntity(Loc.GetString("hypno-max-subject"), uid, uid, PopupType.Large);
                return;
            }

            if (!component.ByPassMindShield && HasComp<MindShieldComponent>(args.Target))
            {
                _popups.PopupEntity(Loc.GetString("has-mindshield"), uid, uid, PopupType.Large);
                return;
            }

            if (HasComp<HypnotizedComponent>(args.Target))
            {
                _popups.PopupEntity(Loc.GetString("hypno-already-under", ("target", uid)), uid, uid, PopupType.Large);
                return;
            }

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.UseDelay, new PsionicHypnosisDoAfterEvent(1), uid, target: args.Target)
            {
                Hidden = true,
                BreakOnTargetMove = true,
                BreakOnDamage = true,
                BreakOnUserMove = true
            }, out var doAfterId);

            component.DoAfter = doAfterId;

            _popups.PopupEntity(Loc.GetString("hypno-start", ("target", args.Target)), uid, uid, PopupType.LargeCaution);
            _popups.PopupEntity(Loc.GetString("hypno-phase-1", ("target", uid)), args.Target, args.Target, PopupType.Small);

            args.Handled = true;
            _psionics.LogPowerUsed(args.Performer, "hypno");
        }

        private void OnDispelled(EntityUid uid, PsionicHypnoComponent component, DispelledEvent args)
        {
            if (component.DoAfter is null)
                return;

            _doAfterSystem.Cancel(component.DoAfter);
            component.DoAfter = null;
            args.Handled = true;
        }

        private void OnDispelledHypnotized(EntityUid uid, HypnotizedComponent component, DispelledEvent args)
        {
            StopHypno(uid, component);
        }

        private void OnMindbreak(EntityUid uid, HypnotizedComponent component, ref OnMindbreakEvent args)
        {
            StopHypno(uid, component);
        }

        private void OnMindShield(EntityUid uid, HypnotizedComponent component, ref ImplantImplantedEvent ev)
        {
            if (_tag.HasTag(ev.Implant, "MindShield") && ev.Implanted != null)
            {
                if (component.Master is not null
                    && TryComp<PsionicHypnoComponent>(component.Master, out var hypnotist)
                    && hypnotist.ByPassMindShield)
                    return;

                StopHypno(uid, component);
            }
        }

        private void ReleaseSubjectVerb(EntityUid uid, PsionicHypnoComponent component, GetVerbsEvent<InnateVerb> args)
        {
            if (args.User == args.Target
                || !TryComp<HypnotizedComponent>(args.Target, out var hypno)
                || args.User != uid)
                return;

            InnateVerb verbReleaseHypno = new()
            {
                Act = () => StopHypno(args.Target),
                Text = Loc.GetString("hypno-release"),
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Floof/Interface/Actions/hypno.png")),
                Priority = 1
            };
            args.Verbs.Add(verbReleaseHypno);
        }

        private void OnDoAfter(EntityUid uid, PsionicHypnoComponent component, PsionicHypnosisDoAfterEvent args)
        {
            if (component is null)
                return;

            component.DoAfter = null;

            if (args.Target is null
                || args.Cancelled)
                return;

            if (args.Phase == 1)
            {
                _popups.PopupEntity(Loc.GetString("hypno-phase-2", ("target", uid)), args.Target.Value, args.Target.Value, PopupType.Medium);

                _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.UseDelay, new PsionicHypnosisDoAfterEvent(2), uid, target: args.Target)
                {
                    Hidden = true,
                    BreakOnTargetMove = true,
                    BreakOnDamage = true,
                    BreakOnUserMove = true
                }, out var doAfterId);
                component.DoAfter = doAfterId;
            }
            else if (args.Phase == 2)
            {
                _popups.PopupEntity(Loc.GetString("hypno-phase-3"), args.Target.Value, args.Target.Value, PopupType.Medium);

                _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.UseDelay, new PsionicHypnosisDoAfterEvent(3), uid, target: args.Target)
                {
                    Hidden = true,
                    BreakOnTargetMove = true,
                    BreakOnDamage = true,
                    BreakOnUserMove = true
                }, out var doAfterId);
                component.DoAfter = doAfterId;
            }
            else
            {
                _popups.PopupEntity(Loc.GetString("hypno-success", ("target", uid)), uid, uid, PopupType.LargeCaution);

                Hypnotize(uid, args.Target.Value, component);
            }
        }

        public void Hypnotize(EntityUid uid, EntityUid target, PsionicHypnoComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            EnsureComp<HypnotizedComponent>(target, out var hypnotized);
            hypnotized.Master = uid;

            Dirty(target, hypnotized);

            component.Subjects += 1;

            _adminLog.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(uid)} hypnotized {ToPrettyString(target)}");

            RaiseLocalEvent(target, new MoodEffectEvent("BeingHypnotized"));

            if (_playerManager.TryGetSessionByEntity(target, out var session)
                || session is not null)
            {
                var message = Loc.GetString("hypnotized", ("entity", uid));
                _chatManager.ChatMessageToOne(
                    ChatChannel.Emotes,
                    message,
                    message,
                    EntityUid.Invalid,
                    false,
                    session.Channel);
            }
        }

        public void StopHypno(EntityUid uid, HypnotizedComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            _adminLog.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(uid)} is not longer hypnotized.");

            _popups.PopupEntity(Loc.GetString("hypno-free"), uid, uid, PopupType.LargeCaution);

            RaiseLocalEvent(uid, new MoodEffectEvent("LostHypnosis"));

            if (_playerManager.TryGetSessionByEntity(uid, out var session)
                || session is not null)
            {
                var message = Loc.GetString("stophypno", ("entity", uid));
                _chatManager.ChatMessageToOne(
                    ChatChannel.Emotes,
                    message,
                    message,
                    EntityUid.Invalid,
                    false,
                    session.Channel);
            }

            if (component.Master is not null
                && TryComp<PsionicHypnoComponent>(component.Master, out var hypnotist))
            {
                hypnotist.Subjects -= 1;
                _popups.PopupEntity(Loc.GetString("lost-subject"), hypnotist.Owner, hypnotist.Owner, PopupType.LargeCaution);
            }

            RemComp(uid, component);
        }

        private void OnExamine(EntityUid uid, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
                args.PushMarkup(Loc.GetString("examined-hypno"), -1);
        }
    }
}


