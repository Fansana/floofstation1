namespace Content.Shared.Floofstation.Traits.Components;

[RegisterComponent]
public sealed partial class FixtureDensityModifierComponent : Component
{
    /// <summary>
    ///     The minimum and maximum density that may be used as input for and achieved as a result of application of this component.
    /// </summary>
    [DataField]
    public float Min = float.Epsilon, Max = float.PositiveInfinity;

    [DataField]
    public float Factor = 1f;
}
