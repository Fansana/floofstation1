using Content.Server.Polymorph.Systems;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Server.Polymorph.Components;

[RegisterComponent]
[Access(typeof(PolymorphSystem))]
public sealed partial class PolymorphProviderComponent : Component
{
    [DataField]
    public ProtoId<PolymorphPrototype> Polymorph;
}
