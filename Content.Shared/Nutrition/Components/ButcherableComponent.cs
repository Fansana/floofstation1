using Content.Shared.Gibbing.Events;
using Content.Shared.Storage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Nutrition.Components
{
    /// <summary>
    /// Indicates that the entity can be thrown on a kitchen spike for butchering.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class ButcherableComponent : Component
    {
        [DataField("spawned", required: true)]
        public List<EntitySpawnEntry> SpawnedEntities = new();

        [ViewVariables(VVAccess.ReadWrite), DataField("butcherDelay")]
        public float ButcherDelay = 8.0f;

        [ViewVariables(VVAccess.ReadWrite), DataField("butcheringType")]
        public ButcheringType Type = ButcheringType.Knife;

        /// <summary>
        /// Prevents butchering same entity on two and more spikes simultaneously and multiple doAfters on the same Spike
        /// </summary>
        [ViewVariables]
        public bool BeingButchered;

        // Floof section begin
        /// <summary>
        ///     Whether the entities body should be gibbed by butchering.
        /// </summary>
        [DataField]
        public bool GibBody = true, GibOrgans = false;

        /// <summary>
        ///     How to handle the contents of the gib.
        /// </summary>
        [DataField]
        public GibContentsOption GibContents = GibContentsOption.Drop;
        // Floof section end
    }

    [Serializable, NetSerializable]
    public enum ButcheringType : byte
    {
        Knife, // e.g. goliaths
        Spike, // e.g. monkeys
        Gibber // e.g. humans. TODO
    }
}
