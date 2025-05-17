using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Alert;

namespace Content.Shared._Floof.Shadekin;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ShadekinComponent : Component
{
    /// <summary>
    ///     Set the Black-Eye Color.
    /// </summary>
    [DataField]
    public Color BlackEyeColor = Color.Black;

    [DataField]
    public Color OldEyeColor = Color.White;

    [DataField]
    public EntityUid? ShadekinSleepAction;

    [DataField, AutoNetworkedField]
    public float Energy = 200;

    [DataField, AutoNetworkedField]
    public float MaxEnergy = 200;

    /// <summary>
    ///     If true, the shadekin is a Blackeye (This also affect spawning).
    /// </summary>
    [DataField]
    public bool Blackeye = true;

    [DataField]
    public ProtoId<AlertPrototype> ShadekinEnergyAlert = "ShadekinEnergyAlert";

    [DataField]
    public ProtoId<AlertPrototype> ShadekinLightAlert = "ShadekinLightAlert";
}
