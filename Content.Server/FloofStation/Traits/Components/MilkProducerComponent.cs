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
public sealed partial class MilkProducerComponent : Component
{
    [DataField("solutionname"), ViewVariables(VVAccess.ReadWrite)]
    public string SolutionName = "breasts";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ReagentPrototype> ReagentId = "Milk";

    [DataField]
    public FixedPoint2 MaxVolume = FixedPoint2.New(50);

    [DataField]
    public Entity<SolutionComponent>? Solution = null;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 QuantityPerUpdate = 5;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HungerUsage = 10f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan GrowthDelay = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextGrowth = TimeSpan.FromSeconds(0);
}
