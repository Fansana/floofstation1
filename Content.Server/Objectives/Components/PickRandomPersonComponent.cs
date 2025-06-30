using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Sets the target for <see cref="TargetObjectiveComponent"/> to a random person.
/// </summary>
[RegisterComponent, Access(typeof(KillPersonConditionSystem))]
public sealed partial class PickRandomPersonComponent : Component
{
    //Floofstation Target Consent Traits: Start
    [DataField]
    public string ObjectiveType = string.Empty;
    //Floofstation Target Consent Traits: End
}
