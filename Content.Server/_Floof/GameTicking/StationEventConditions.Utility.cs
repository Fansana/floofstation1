using System.Linq;
using Content.Server.StationEvents.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Floof.GameTicking;

/// <summary>
///     Combines a number of other conditions in a boolean AND or a boolean OR.
/// </summary>
/// <example>
///     <code>
///         - !type:ComplexCondition
///           requireAll: true
///           conditions:
///           - !type:SomeCondition1
///             ...
///           - !type:SomeCondition2
///             ...
///     </code>
/// </example>
[Serializable]
public sealed partial class ComplexCondition : StationEventCondition
{
    /// <summary>
    ///     If true, this condition acts as a boolean AND. If false, it acts as a boolean OR.
    /// </summary>
    [DataField]
    public bool RequireAll = false;

    [DataField(required: true)]
    public List<StationEventCondition> Conditions = new();

    public override bool IsMet(EntityPrototype proto, StationEventComponent component, Dependencies dependencies) =>
        RequireAll
            ? Conditions.All(it => it.Inverted ^ it.IsMet(proto, component, dependencies))
            : Conditions.Any(it => it.Inverted ^ it.IsMet(proto, component, dependencies));
}
