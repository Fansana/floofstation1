namespace Content.Server.NPC.Components;

[RegisterComponent]
public sealed partial class NpcFactionSelectorComponent : Component
{
    [DataField]
    public List<string> SelectableFactions = new();
}

