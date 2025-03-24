using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Floofstation.Traits.Components;

namespace Content.Shared.Floofstation.Traits;

public sealed class SizeTraitSystem : EntitySystem
{
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SizeTraitComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<SizeTraitComponent, ComponentStartup>(SizeTraitStartup);
    }
    
    // Modify damage taken
    private void OnDamageModify(EntityUid uid, SizeTraitComponent component, DamageModifyEvent args)
    {
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, component.DamageModifiers);
    }
    
    private void SizeTraitStartup(EntityUid uid, SizeTraitComponent component, ComponentStartup args)
    {
        if (TryComp<MobThresholdsComponent>(uid, out var threshold))
        {
            // modify critical damage threshold
            var critThreshold = _threshold.GetThresholdForState(uid, Mobs.MobState.Critical, threshold); 
            if (critThreshold != 0)
                _threshold.SetMobStateThreshold(uid, critThreshold + component.CritThresholdModifier, Mobs.MobState.Critical);
            
            // modify death damage threshold
            var deadThreshold = _threshold.GetThresholdForState(uid, Mobs.MobState.Dead, threshold);
            if (deadThreshold != 0)
                _threshold.SetMobStateThreshold(uid, deadThreshold + component.DeadThresholdModifier, Mobs.MobState.Dead);
        }

        // modify stamina crit threshold
        if (TryComp<StaminaComponent>(uid, out var stamina))
            stamina.CritThreshold += component.CritThresholdModifier;
        
    }
    
    /* // Modify critical damage threshold
    private void OnCritStartup(EntityUid uid, SizeTraitComponent component, ComponentStartup args)
    {
        if (!TryComp<MobThresholdsComponent>(uid, out var threshold))
            return;

        var critThreshold = _threshold.GetThresholdForState(uid, Mobs.MobState.Critical, threshold);
        if (critThreshold != 0)
            _threshold.SetMobStateThreshold(uid, critThreshold + component.CritThresholdModifier, Mobs.MobState.Critical);
    }
    
    // Modify death damage threshold
    private void OnDeadStartup(EntityUid uid, SizeTraitComponent component, ComponentStartup args)
    {
        if (!TryComp<MobThresholdsComponent>(uid, out var threshold))
            return;

        var deadThreshold = _threshold.GetThresholdForState(uid, Mobs.MobState.Dead, threshold);
        if (deadThreshold != 0)
            _threshold.SetMobStateThreshold(uid, deadThreshold + component.DeadThresholdModifier, Mobs.MobState.Dead);
    }
    
    // Modify stamina crit threshold
    private void OnStaminaCritStartup(EntityUid uid, SizeTraitComponent component, ComponentStartup args)
    {
        if (!TryComp<StaminaComponent>(uid, out var stamina))
            return;

        stamina.CritThreshold += component.CritThresholdModifier;
    } */
}