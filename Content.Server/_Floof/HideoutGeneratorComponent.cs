using Robust.Shared.Map;


namespace Content.Server._Floof;


[RegisterComponent]
public sealed partial class HideoutGeneratorComponent : Component
{

    /// <summary>
    /// Maps we've generated.
    /// </summary>
    [DataField]
    public List<MapId> Generated = new();

}
