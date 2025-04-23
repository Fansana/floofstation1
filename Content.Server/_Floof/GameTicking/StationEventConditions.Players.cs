using Content.Server.StationEvents.Components;
using Content.Shared.InteractionVerbs;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.FloofStation.GameTicking;

/// <summary>
///     A condition that requires a number of players to be present in a specific department.
/// </summary>
/// <example><code>
///     - !type:DepartmentCountCondition
///       department: Security
///       range: {min: 5}
/// </code></example>
[Serializable]
public sealed partial class DepartmentCountCondition : StationEventCondition
{
    [DataField(required: true)]
    public ProtoId<DepartmentPrototype> Department;

    [DataField(required: true)]
    public InteractionVerbPrototype.RangeSpecifier Range;

    public override bool IsMet(EntityPrototype proto, StationEventComponent component, Dependencies dependencies)
    {
        var count = dependencies.DeptCounts.GetValueOrDefault(Department, 0);
        return Range.IsInRange(count);
    }
}

/// <summary>
///     Same as <see cref="DepartmentCountCondition"/>, but for specific jobs.
/// </summary>
[Serializable]
public sealed partial class JobCountCondition : StationEventCondition
{
    [DataField(required: true)]
    public ProtoId<JobPrototype> Job;

    [DataField(required: true)]
    public InteractionVerbPrototype.RangeSpecifier Range;

    public override bool IsMet(EntityPrototype proto, StationEventComponent component, Dependencies dependencies)
    {
        var count = dependencies.JobCounts.GetValueOrDefault(Job, 0);
        return Range.IsInRange(count);
    }
}
