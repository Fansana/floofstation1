using Robust.Shared.Audio;

namespace Content.Server.FloofStation;

[RegisterComponent]
public sealed partial class VoredComponent : Component
{
    [DataField]
    public EntityUid Pred;

    [DataField]
    public bool Digested = false;

    public SoundSpecifier? SoundDigestion = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };
}
