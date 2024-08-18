namespace Content.Shared.Floofstation.Leash.Components;

[RegisterComponent]
public sealed partial class LeashedComponent : Component
{
    [DataField]
    public string? JointId = null;

    [NonSerialized]
    public EntityUid? Puller = null;
}
