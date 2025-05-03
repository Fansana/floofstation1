using Content.Shared.Access;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;


namespace Content.Shared._Floof.Lock;


/// <summary>
///     When applied to a lockable entity, allows installing an adittional layer of security upon it
///     by swiping an id across id, preventing anyone without the same name/job on their ID from unlocking it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IdLockComponent : Component
{
    /// <summary>
    ///     Whether this lock is enabled. A disabled ID lock will not affect the entity in any way.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    ///     Whether this lockable entity has an active ID-lock.
    ///     While active, the ID lock prevents the regular lock from being opened.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LockState State = LockState.Disengaged;

    /// <summary>
    ///     The information about the last ID used to lock this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LockInfo Info;

    /// <summary>
    ///     If true, the locked entity will reveal who it was locked by.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RevealInfo = true;

    /// <summary>
    ///     "Master" access types. If the user has any of these, they can unlock the lock regardless of whether it was locked by them.
    /// </summary>
    [DataField, AutoNetworkedField, AlwaysPushInheritance]
    public HashSet<ProtoId<AccessLevelPrototype>> MasterAccesses = new();

    [DataField, AutoNetworkedField]
    public TimeSpan LockTime = TimeSpan.FromSeconds(1), UnlockTime = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     Time to unlock the lock using a master ID.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan MasterUnlockTime = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? LockSound, UnlockSound;

    [DataDefinition, Serializable, NetSerializable]
    public partial struct LockInfo
    {
        [DataField]
        public string? OwnerName;

        [DataField]
        public string? OwnerJobTitle;
    }

    [Serializable, NetSerializable]
    public enum LockState
    {
        /// <summary>
        ///     Locked.
        /// </summary>
        Engaged,
        /// <summary>
        ///     Unlocked.
        /// </summary>
        Disengaged,
        /// <summary>
        ///     Opened via a master ID, can be locked again without erasing the owner info.
        /// </summary>
        TemporarilyDisengaged
    }
}
