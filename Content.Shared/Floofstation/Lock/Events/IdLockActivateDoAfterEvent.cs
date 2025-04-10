using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.FloofStation.Lock.Events;

[Serializable, NetSerializable]
public sealed partial class IdLockActivateDoAfterEvent : SimpleDoAfterEvent;
