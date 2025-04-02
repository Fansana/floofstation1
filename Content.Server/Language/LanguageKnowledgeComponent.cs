using Content.Shared.Language;
using Robust.Shared.Prototypes;

namespace Content.Server.Language;

/// <summary>
///     Stores data about entities' intrinsic language knowledge.
/// </summary>
[RegisterComponent]
public sealed partial class LanguageKnowledgeComponent : Component
{
    /// <summary>
    ///     List of languages this entity can speak without any external tools.
    /// </summary>
    [DataField("speaks", required: true)]
    public List<ProtoId<LanguagePrototype>> SpokenLanguages = new();

    /// <summary>
    ///     List of languages this entity can understand without any external tools.
    /// </summary>
    [DataField("understands", required: true)]
    public List<ProtoId<LanguagePrototype>> UnderstoodLanguages = new();

    /// <summary>
    ///     Floof: Traits can replace or reference natural given languages, define this for roundstart species that have a natural language
    ///     The entity's base natural language. Not guaranteed the entity knows this, but this is the protoype's default.
    /// </summary>
    [DataField("naturalLanguage", required: false)]
    public ProtoId<LanguagePrototype>? NaturalLanguage = default!;
}
