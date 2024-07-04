using Content.Shared.Actions;
using Content.Shared.StatusEffect;
using Content.Shared.Psionics.Abilities;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Damage;
using Content.Shared.Revenant.Components;
using Content.Server.Guardian;
using Content.Server.Bible.Components;
using Content.Server.Popups;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Shared.Actions.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Psionics.Abilities
{
    public sealed class DispelPowerSystem : EntitySystem
    {
        [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly GuardianSystem _guardianSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedPsionicAbilitiesSystem _psionics = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DispelPowerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<DispelPowerComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<DispelPowerActionEvent>(OnPowerUsed);

            SubscribeLocalEvent<DispellableComponent, DispelledEvent>(OnDispelled);
            SubscribeLocalEvent<DamageOnDispelComponent, DispelledEvent>(OnDmgDispelled);
            // Upstream stuff we're just gonna handle here
            SubscribeLocalEvent<GuardianComponent, DispelledEvent>(OnGuardianDispelled);
            SubscribeLocalEvent<FamiliarComponent, DispelledEvent>(OnFamiliarDispelled);
            SubscribeLocalEvent<RevenantComponent, DispelledEvent>(OnRevenantDispelled);
        }

        private void OnInit(EntityUid uid, DispelPowerComponent component, ComponentInit args)
        {
            _actions.AddAction(uid, ref component.DispelActionEntity, component.DispelActionId );
            _actions.TryGetActionData( component.DispelActionEntity, out var actionData );
            if (actionData is { UseDelay: not null })
                _actions.StartUseDelay(component.DispelActionEntity);
            if (TryComp<PsionicComponent>(uid, out var psionic))
            {
                psionic.ActivePowers.Add(component);
                psionic.PsychicFeedback.Add(component.DispelFeedback);
                //It's fully intended that Dispel doesn't increase Amplification, and instead heavily spikes Dampening
                //Antimage archetype.
                psionic.Dampening += 1f;
            }
        }

        private void OnShutdown(EntityUid uid, DispelPowerComponent component, ComponentShutdown args)
        {
            _actions.RemoveAction(uid, component.DispelActionEntity);

            if (TryComp<PsionicComponent>(uid, out var psionic))
            {
                psionic.ActivePowers.Remove(component);
                psionic.PsychicFeedback.Remove(component.DispelFeedback);
                psionic.Dampening -= 1f;
            }
        }

        private void OnPowerUsed(DispelPowerActionEvent args)
        {
            if (HasComp<PsionicInsulationComponent>(args.Target) || HasComp<PsionicInsulationComponent>(args.Performer))
                return;
            if (!TryComp<PsionicComponent>(args.Performer, out var psionic) || !HasComp<PsionicComponent>(args.Target))
                return;

            var ev = new DispelledEvent();
            RaiseLocalEvent(args.Target, ev, false);

            if (ev.Handled)
            {
                args.Handled = true;
                _psionics.LogPowerUsed(args.Performer, "dispel", psionic, 1, 1, true);

                // Redundant check here as well for a small performance optimization
                // Dispel has its own niche equations for glimmer.
                if (_glimmerSystem.GetGlimmerEnabled())
                    _glimmerSystem.DeltaGlimmerOutput(-_random.NextFloat(2 * psionic.Dampening - psionic.Amplification, 4 * psionic.Dampening - psionic.Amplification));
            }
        }

        private void OnDispelled(EntityUid uid, DispellableComponent component, DispelledEvent args)
        {
            QueueDel(uid);
            Spawn("Ash", Transform(uid).Coordinates);
            _popupSystem.PopupCoordinates(Loc.GetString("psionic-burns-up", ("item", uid)), Transform(uid).Coordinates, Filter.Pvs(uid), true, Shared.Popups.PopupType.MediumCaution);
            _audioSystem.PlayEntity("/Audio/Effects/lightburn.ogg", Filter.Pvs(uid), uid, true);
            args.Handled = true;
        }

        private void OnDmgDispelled(EntityUid uid, DamageOnDispelComponent component, DispelledEvent args)
        {
            var damage = component.Damage;
            var modifier = 1 + component.Variance - _random.NextFloat(0, component.Variance * 2);

            damage *= modifier;
            DealDispelDamage(uid, damage);
            args.Handled = true;
        }

        private void OnGuardianDispelled(EntityUid uid, GuardianComponent guardian, DispelledEvent args)
        {
            if (TryComp<GuardianHostComponent>(guardian.Host, out var host))
                _guardianSystem.ToggleGuardian(guardian.Host.Value, host);

            DealDispelDamage(uid);
            args.Handled = true;
        }

        private void OnFamiliarDispelled(EntityUid uid, FamiliarComponent component, DispelledEvent args)
        {
            if (component.Source != null)
                EnsureComp<SummonableRespawningComponent>(component.Source.Value);

            args.Handled = true;
        }

        private void OnRevenantDispelled(EntityUid uid, RevenantComponent component, DispelledEvent args)
        {
            DealDispelDamage(uid);
            _statusEffects.TryAddStatusEffect(uid, "Corporeal", TimeSpan.FromSeconds(30), false, "Corporeal");
            args.Handled = true;
        }

        public void DealDispelDamage(EntityUid uid, DamageSpecifier? damage = null)
        {
            if (Deleted(uid))
                return;

            _popupSystem.PopupCoordinates(Loc.GetString("psionic-burn-resist", ("item", uid)), Transform(uid).Coordinates, Filter.Pvs(uid), true, Shared.Popups.PopupType.SmallCaution);
            _audioSystem.PlayEntity("/Audio/Effects/lightburn.ogg", Filter.Pvs(uid), uid, true);

            if (damage == null)
            {
                damage = new();
                damage.DamageDict.Add("Blunt", 100);
            }
            _damageableSystem.TryChangeDamage(uid, damage, true, true);
        }
    }
    public sealed class DispelledEvent : HandledEntityEventArgs {}
}
