using Robust.Shared.Serialization;

namespace Content.Shared.NPC.Events;

/// <summary>
/// Raised from client to server to notify a faction was added to an NPC.
/// </summary>
[Serializable, NetSerializable]
public sealed class NpcFactionAddedEvent : EntityEventArgs
{
    public readonly string FactionID;
    public readonly NetEntity EntityUid;

    public NpcFactionAddedEvent(NetEntity entity, string factionId)
    {
        FactionID = factionId;
        EntityUid = entity;
    }
}

/// <summary>
/// Raised from client to server to notify a faction was removed from an NPC.
/// </summary>
[Serializable, NetSerializable]
public sealed class NpcFactionRemovedEvent : EntityEventArgs
{
    public readonly string FactionID;
    public readonly NetEntity EntityUid;

    public NpcFactionRemovedEvent(NetEntity entity, string factionId)
    {
        FactionID = factionId;
        EntityUid = entity;
    }
}
