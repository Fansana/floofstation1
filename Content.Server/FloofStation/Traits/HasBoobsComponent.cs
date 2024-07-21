using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.FloofStation.Traits;

[RegisterComponent, Access(typeof(HasBoobsSystem))]
public sealed partial class HasBoobsComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<ReagentPrototype> ReagentId = "Milk";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string SolutionName = "breasts";

    [DataField]
    public Entity<SolutionComponent>? Solution = null;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 QuantityPerUpdate = 25;

    [DataField]
    public FixedPoint2 CumMaxVolume = FixedPoint2.New(200);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HungerUsage = 10f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan GrowthDelay = TimeSpan.FromMinutes(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextGrowth = TimeSpan.FromSeconds(0);
}

