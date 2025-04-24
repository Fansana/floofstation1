namespace Content.Shared._Floof.Lock.Events;

/// <summary>
///     Raised on a lockable entity to add/remove an ID lock.
///
///     Primarily meant to allow for construction graphs to upgrade an entity (such as a locker) to an ID-lockable version.
/// </summary>
[Serializable, DataDefinition]
public sealed partial class IdLockSetEvent : EntityEventArgs
{
    [DataField]
    public bool Enable;
}
