using Content.Shared.Language;
using Content.Shared.Language.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.FloofStation.Traits;

/// <summary>
///     When applied to a not-yet-spawned player entity, removes <see cref="BaseLanguage"/> from the lists of their languages
///     and gives them a translator instead.
/// </summary>
[RegisterComponent]
public sealed partial class NaturalSpeakerTraitComponent : Component
{
    /// <summary>
    ///     The language that will be given with the trait.
    /// </summary>
    [DataField]
    public ProtoId<LanguagePrototype> Language = SharedLanguageSystem.FallbackLanguagePrototype;
}
