using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Lock.Events;

[Serializable, NetSerializable]
public sealed partial class IdLockActivateDoAfterEvent : SimpleDoAfterEvent;
