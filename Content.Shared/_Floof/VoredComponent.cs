using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.FloofStation;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VoredComponent : Component
{
    public EntityUid? Stream;
    public float Accumulator;

    [DataField, AutoNetworkedField]
    public EntityUid Pred;

    [DataField]
    public bool Digesting = false;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundBelly = new SoundPathSpecifier("/Audio/_Floof/Vore/stomach_loop.ogg")
    {
        Params = AudioParams.Default.WithLoop(true).WithVolume(-4f),
    };

    [DataField]
    public SoundSpecifier? SoundRelease = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg")
    {
        Params = AudioParams.Default.WithVolume(-4f),
    };
}
