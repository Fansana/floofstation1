using System.Linq;
using Content.Server.Hands.Systems;
using Content.Server.Language;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Language;
using Content.Shared.Language.Components;
using Content.Shared.Language.Components.Translators;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Server._Floof.Traits;


public sealed partial class NaturalSpeakerTraitSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entMan = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly LanguageSystem _languages = default!;
    [Dependency] private readonly StorageSystem _storage = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NaturalSpeakerTraitComponent, ComponentInit>(OnSpawn); // TraitSystem adds it after PlayerSpawnCompleteEvent so it's fine
    }

    private void OnSpawn(Entity<NaturalSpeakerTraitComponent> entity, ref ComponentInit args)
    {
        if (!TryComp<LanguageKnowledgeComponent>(entity, out var knowledge))
        {
            Log.Warning($"Entity {entity.Owner} does not have a LanguageKnowledge but has a NaturalSpeakerTrait!");
            return;
        }

        var targetLanguage = knowledge.NaturalLanguage;
        if (targetLanguage == null)
        {
            Log.Warning($"Entity {entity.Owner} does not have a natural language, so NaturalSpeakerTrait is providing one for free");
        }
        else
        {
            _languages.RemoveLanguage(entity.Owner, targetLanguage.Value, true, true);
        }
        _languages.AddLanguage(entity.Owner, entity.Comp.Language, true, true);
    }
}
