using Content.Shared.Traits;
using Robust.Shared.Serialization.Manager;
using Content.Shared.HeightAdjust;

namespace Content.Server.FloofStation.Traits;

// Scales/modifies the size of the character using the Floofstation modified heightAdjustSystem function SetScale
public sealed partial class TraitSetScale : TraitFunction 
{
    [DataField]
    public float scale;

    public override void OnPlayerSpawn(EntityUid uid,
        IComponentFactory factory,
        IEntityManager entityManager,
        ISerializationManager serializationManager)
    { 
        entityManager.System<HeightAdjustSystem>().SetScale(uid, scale, restricted: false);
    }
}