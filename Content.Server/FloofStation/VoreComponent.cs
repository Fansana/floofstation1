using Robust.Shared.Containers;
using Robust.Shared.Audio;

namespace Content.Server.FloofStation;

[RegisterComponent]
public sealed partial class VoreComponent : Component
{
    [DataField]
    public SoundSpecifier? SoundDevour = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };
    public Container Stomach = default!;
}
