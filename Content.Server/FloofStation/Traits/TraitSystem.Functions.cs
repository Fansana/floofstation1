using Content.Shared.Traits;
//using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager;
using Robust.Server.GameObjects; // Floofstation
using System.Numerics; //Floofstation

namespace Content.Server.FloofStation.Traits;

// Scales/modifies the visual size of the character. DOES NOT MODIFY DENSITY/WEIGHT. Use in conjunction with FixtureDensityModifier component.
//[UsedImplicitly]
public sealed partial class TraitScaleVisualSize : TraitFunction 
{
    [DataField]
    public Vector2 scale;

    public override void OnPlayerSpawn(EntityUid uid,
        IComponentFactory factory,
        IEntityManager entityManager,
        ISerializationManager serializationManager)
    {
       // Make sure the player has the ScaleVisuals Component, it's required to activate the ScaleVisualsSystem
       entityManager.EnsureComponent<ScaleVisualsComponent>(uid);
       
       // Apply the new scale, generate the AppearanceComponent if required
       entityManager.System<AppearanceSystem>().SetData(uid, ScaleVisuals.Scale, scale, entityManager.EnsureComponent<AppearanceComponent>(uid));
    }
}