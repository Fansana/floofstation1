using Content.Shared.Damage;

namespace Content.Shared.Floofstation.Traits.Components;


// modifiers for use with size modification traits
// TODO: add prying speed, melee damage, and stamina damage modifiers
[RegisterComponent]
public sealed partial class SizeTraitComponent : Component
{
    [DataField]
    public DamageModifierSet DamageModifiers = default!;
    
    [DataField]
    public int CritThresholdModifier = 0;
    
    [DataField]
    public int StaminaCritThresholdModifier = 0;
    
    [DataField]
    public int DeadThresholdModifier = 0;
    
}