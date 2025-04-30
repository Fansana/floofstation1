using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared.Overlays.Switchable;

[RegisterComponent, NetworkedComponent]
public sealed partial class NightVisionComponent : SwitchableOverlayComponent
{
    public override string? ToggleAction { get; set; } = "ToggleNightVision";

    public override Color Color { get; set; } = Color.FromHex("#98FB98");
    
    /// <summary>
    ///     Determines how intense the visual artifacting will be.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float NvdSnow { get; set; } = 1f;

    /// <summary>
    ///     Determines whether or not the night vision will only see in greyscale.
    ///     This means it will only see in shades of it's color tint.
    ///     1 for on, 0 for off.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Darkvision { get; set; } = 1f;
}

public sealed partial class ToggleNightVisionEvent : InstantActionEvent;
