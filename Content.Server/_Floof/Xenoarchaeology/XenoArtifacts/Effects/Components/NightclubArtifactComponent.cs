using Content.Server._Floof.Xenoarchaeology.XenoArtifacts.Effects.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Floof.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Artifact replaces all nearby lights with colorful variants
/// </summary>
[RegisterComponent]
public sealed partial class NightclubArtifactComponent : Component
{
    /// <summary>
    /// A list of light prototypes to use to replace pre-existing lights
    /// TODO: Replace this with either an EntityTable or something better that is pre-existing.
    /// </summary>
    [DataField]
    public List<String> Replacements = new List<String>();

    /// <summary>
    /// range of the effect.
    /// </summary>
    [DataField]
    public float Range = 5f;

    /// <summary>
    /// Sound to play on polymorph.
    /// </summary>
    [DataField]
    public SoundSpecifier PolySound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/Magic/staff_animation.ogg");
}
