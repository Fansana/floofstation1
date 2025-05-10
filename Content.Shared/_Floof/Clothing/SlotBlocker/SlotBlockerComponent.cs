using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;


namespace Content.Shared._Floof.Clothing.SlotBlocker;


/// <summary>
///     Applied to clothing that can block and be blocked by other clothing.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlotBlockerComponent : Component
{
    [DataField, AutoNetworkedField, AlwaysPushInheritance]
    public BlockerDefinition Blocks, BlockedBy;

    [DataField]
    public bool IgnoreOtherBlockers = false;
}

[DataDefinition, Serializable, NetSerializable]
public partial struct BlockerDefinition
{
    /// <summary>
    ///     Slots that block this clothing or get blocked by this clothing.
    /// </summary>
    [DataField]
    public SlotFlags Slots = SlotFlags.NONE;

    [DataField]
    public EntityWhitelist? Whitelist, Blacklist;

    [DataField]
    public bool PreventsEquip = true, PreventsUnequip = true;

    /// <summary>
    ///     Will only prevent equipping/unequipping ONLY if the BLOCKER is in one of these slots (not the equipment).
    ///     Excludes pockets by default.
    /// </summary>
    [DataField]
    public SlotFlags EnableInSlots = SlotFlags.WITHOUT_POCKET;
}
