using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Floofstation.Leash.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LeashComponent : Component
{
    /// <summary>
    ///     Maximum number of leash joints that this entity can create.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxJoints = 1;

    /// <summary>
    ///     Default length of the leash joint.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Length = 3.5f;

    /// <summary>
    ///     Maximum distance between the anchor and the puller beyond which the leash will break.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxDistance = 8f;

    /// <summary>
    ///     The time it takes for one entity to attach the leash to another entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AttachDelay = TimeSpan.FromSeconds(2f), DetachDelay = TimeSpan.FromSeconds(2f);

    /// <summary>
    ///     The time it takes for the leashed entity to detach itself.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SelfDetachDelay = TimeSpan.FromSeconds(8f);

    [DataField, AutoNetworkedField]
    public SpriteSpecifier? LeashSprite;

    /// <summary>
    ///     How much damage each leash joint can sustain before it breaks.
    /// </summary>
    /// <remarks>Damage here actually refers to impulse exerted by the joint minus repair.</remarks>
    [DataField, AutoNetworkedField]
    public float BreakDamage = 20f;

    /// <summary>
    ///     How much damage each leash joint loses every <see cref="DamageInterval"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float JointRepairDamage = 1f;

    /// <summary>
    ///     Interval at which damage is calculated for each joint.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DamageInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>
    ///     List of all joints and their respective pulled entities created by this leash.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<LeashData> Leashed = new();

    [DataDefinition, Serializable, NetSerializable]
    public partial class LeashData
    {
        [DataField]
        public string JointId = string.Empty;

        [DataField]
        public NetEntity Pulled = NetEntity.Invalid;

        /// <summary>
        ///     Entity used to visualize the leash. Created dynamically.
        /// </summary>
        [DataField]
        public NetEntity? LeashVisuals = null;

        [DataField]
        public float Damage = 0f;

        [DataField]
        public TimeSpan NextDamage = TimeSpan.Zero;

        public LeashData(string jointId, NetEntity pulled)
        {
            JointId = jointId;
            Pulled = pulled;
        }
    };
}
