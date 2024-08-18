using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Floofstation.Leash.Events;

[Serializable, NetSerializable]
public sealed partial class LeashAttachDoAfterEvent : SimpleDoAfterEvent
{
}
