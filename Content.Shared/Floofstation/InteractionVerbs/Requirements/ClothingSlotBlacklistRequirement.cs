using Content.Shared.InteractionVerbs;
using Content.Shared.Inventory;
using Robust.Shared.Serialization;

namespace Content.Shared.Floofstation.InteractionVerbs.Requirements;


/// <summary>
///     Requires a mob to not be wearing anything in this slot.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ClothingSlotBlacklistRequirement : InvertableInteractionRequirement
{
    [DataField] public string Slot;

    public override bool IsMet(InteractionArgs args, InteractionVerbPrototype proto, InteractionAction.VerbDependencies deps)
    {
        return !deps.EntMan.System<InventorySystem>().TryGetSlotEntity(args.Target, Slot, out _);
    }
}
