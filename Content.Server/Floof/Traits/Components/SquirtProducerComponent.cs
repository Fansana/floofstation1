using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FloofStation.Traits;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.GameStates;

namespace Content.Server.FloofStation.Traits;

[RegisterComponent, Access(typeof(LewdTraitSystem))]
public sealed partial class SquirtProducerComponent : Component
{
    [DataField("solutionname")]
    public string SolutionName = "vagina";

    [DataField]
    public ProtoId<ReagentPrototype> ReagentId = "NaturalLubricant";

    [DataField]
    public FixedPoint2 MaxVolume = FixedPoint2.New(25);

    [DataField]
    public Entity<SolutionComponent>? Solution = null;

    [DataField]
    public FixedPoint2 QuantityPerUpdate = 5;

    [DataField]
    public float HungerUsage = 10f;

    [DataField]
    public TimeSpan GrowthDelay = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextGrowth = TimeSpan.FromSeconds(0);
}
