using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed partial class MindSwapPowerComponent : Component
    {
        [DataField]
        public float UseDelay = 5f;
    }

    [Serializable, NetSerializable]
    public sealed partial class MindSwapPowerDoAfterEvent : DoAfterEvent
    {
        public override DoAfterEvent Clone() => this;
    }

}
