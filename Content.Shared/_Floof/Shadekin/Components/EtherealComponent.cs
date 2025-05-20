using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;


namespace Content.Shared._Floof.Shadekin;

[RegisterComponent, NetworkedComponent]
public sealed partial class EtherealComponent : Component
{
    /// Can this be stunned by ethereal stun objects?
    [DataField]
    public bool CanBeStunned = true;

    public int OldMobMask;

    public int OldMobLayer;

    public List<ProtoId<NpcFactionPrototype>> SuppressedFactions = new();
    public bool HasDoorBumpTag;
}
