using Robust.Shared.GameStates;

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
    public EntityUid? ShadekinPhaseAction;

    [DataField]
    public EntityUid? ShadekinSleepAction;

    [DataField, AutoNetworkedField]
    public float Energy = 200;

    [DataField, AutoNetworkedField]
    public float MaxEnergy = 200;

    public float Accumulator;

    [DataField]
    public float Energymultiplier = 1;

    [DataField]
    public float LightExposure = 0;

    /// <summary>
    ///     If true, the shadekin is a Blackeye (This also affect spawning).
    /// </summary>
    [DataField]
    public bool Blackeye = true;

    [DataField]
    public bool Rejuvenating = false;
}
