using Robust.Shared.Audio;

namespace Content.Server.FloofStation;

[RegisterComponent]
public sealed partial class HypnoClothingComponent : Component
{
    [DataField]
    public EntityUid? Master;

    [DataField]
    public SoundSpecifier LinkSound = new SoundPathSpecifier("/Audio/Machines/terminal_insert_disc.ogg");
}
