using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Repairable;

public abstract partial class SharedRepairableSystem : EntitySystem
{
    // Floof - made public because WHY THE FUCK WAS THIS PRIVATE?!
    [Serializable, NetSerializable]
    public sealed partial class RepairFinishedEvent : SimpleDoAfterEvent
    {
    }
}

