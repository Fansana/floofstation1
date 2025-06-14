using Content.Shared.Damage;
using Content.Shared.InteractionVerbs;
using Robust.Shared.Serialization;

namespace Content.Server.InteractionVerbs.Actions;

[Serializable]
public sealed partial class ModifyHealthAction : InteractionAction
{
    [DataField(required: true)] public DamageSpecifier Damage = default!;
    [DataField] public bool IgnoreResistance = false;

    /// <summary>
    ///     Floofstation - a random factor applied to the damage.
    /// </summary>
    [DataField] public InteractionVerbPrototype.RangeSpecifier RandomFactor = new() { Min = 0.75f, Max = 1.25f };

    public override bool IsAllowed(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        return deps.EntMan.HasComponent<DamageableComponent>(args.Target);
    }

    public override bool CanPerform(InteractionArgs args, InteractionVerbPrototype proto, bool beforeDelay, VerbDependencies deps)
    {
        // TODO: check if container supports this kind of damage?
        return true;
    }

    public override bool Perform(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        var damage = Damage * RandomFactor.Random(deps.Random); // Floof
        return deps.EntMan.System<DamageableSystem>()
            .TryChangeDamage(args.Target, damage, IgnoreResistance, origin: args.User) is not null; // Floof - changed Damage to damage
    }
}
