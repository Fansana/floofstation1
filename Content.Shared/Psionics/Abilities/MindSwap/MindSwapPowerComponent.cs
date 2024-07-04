using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Psionics.Abilities
{
    [RegisterComponent]
    public sealed partial class MindSwapPowerComponent : Component
    {
        [DataField("mindSwapActionId",
        customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? MindSwapActionId = "ActionMindSwap";

        [DataField("mindSwapActionEntity")]
        public EntityUid? MindSwapActionEntity;

        [DataField("mindSwapFeedback")]
        public string MindSwapFeedback = "mind-swap-feedback";
    }
}
