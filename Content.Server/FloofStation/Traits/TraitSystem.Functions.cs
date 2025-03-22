using Content.Shared.Traits;
using Robust.Shared.Serialization.Manager;
using Robust.Server.GameObjects;
using System.Numerics;
using Content.Shared.HeightAdjust;
using Content.Shared.Humanoid;

namespace Content.Server.FloofStation.Traits;

// Scales/modifies the visual size of the character. DOES NOT MODIFY DENSITY/WEIGHT. Use in conjunction with FixtureDensityModifier component.
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
        // get current player size and adjust based on the scale
        var appearance = entityManager.EnsureComponent<HumanoidAppearanceComponent>(uid);
        float height = appearance.Height * scale;
        float width = appearance.Width * scale;
        
        // Does the actual size adjustment!
        entityManager.System<HeightAdjustSystem>().SetScale(uid, new Vector2(height, width), false);
    }
}