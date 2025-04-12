namespace Content.Server.FloofStation.Paint;


/// <summary>
///     When applied to a component, gives it the specified spray paint color on map init.
/// </summary>
[RegisterComponent]
public sealed partial class PaintOnInitComponent : Component
{
    [DataField]
    public Color? Color;
}
