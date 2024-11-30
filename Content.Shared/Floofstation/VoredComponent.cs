using Robust.Shared.Audio;

namespace Content.Shared.FloofStation;

[RegisterComponent]
public sealed partial class VoredComponent : Component
{
    public float Accumulator;

    [DataField]
    public EntityUid Pred;

    [DataField]
    public bool Digesting = false;

    public SoundSpecifier? SoundBelly = new SoundPathSpecifier("/Audio/Floof/Vore/stomach_loop.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier? SoundRelease = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };
}
