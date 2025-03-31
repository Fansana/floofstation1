using Content.Server.FloofStation.Xenoarchaeology.XenoArtifacts.Effects.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.FloofStation.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// This effect causes the artifact to replace tiles around it.
/// </summary>
[RegisterComponent]
public sealed partial class ReplaceFloorArtifactComponent : Component
{
    /// <summary>
    /// The name of the tile types to replace tiles with
    /// </summary>
    [DataField]
    public string Replacement = "Plating";

    /// <summary>
    /// Radius range of the effect.
    /// </summary>
    [DataField]
    public float Range = 3f;

    /// <summary>
    /// Fall off factor. This value divided by the distance from the artifact, equals the likelihood of the tile being replaced.
    /// Higher values mean replacement is more likely.
    /// Values >=1 mean the first circle of tiles from the artifact are guarunteed to be filled, >=2 means the first two cirlces of tiles, etc.
    /// </summary>
    [DataField]
    public float Falloff = 1.25f;

    /// <summary>
    /// Sound to play when replacing the tiles
    /// </summary>
    [DataField]
    public SoundSpecifier PolySound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/Magic/staff_animation.ogg");
}
