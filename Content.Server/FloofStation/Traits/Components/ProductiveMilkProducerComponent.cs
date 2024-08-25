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
public sealed partial class ProductiveMilkProducerComponent : Component
{
    [DataField("solutionname")]
    public string SolutionName = "breasts";

    [DataField]
    public ProtoId<ReagentPrototype> ReagentId = "Milk";

    [DataField]
    public FixedPoint2 MaxVolume = FixedPoint2.New(100);

    [DataField]
    public Entity<SolutionComponent>? Solution = null;

    [DataField]
    public FixedPoint2 QuantityPerUpdate = 10;

    [DataField]
    public float HungerUsage = 10f;

    [DataField]
    public TimeSpan GrowthDelay = TimeSpan.FromSeconds(5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextGrowth = TimeSpan.FromSeconds(0);
}
