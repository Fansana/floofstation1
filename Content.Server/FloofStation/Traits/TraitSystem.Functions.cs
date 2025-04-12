using Content.Shared.Traits;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Content.Server.Body.Systems;
using Content.Server.Body.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Prototypes;
using Content.Shared.HeightAdjust;
using System.Linq;

namespace Content.Server.FloofStation.Traits;

// FLOOF START
[UsedImplicitly]
public sealed partial class TraitModifyMetabolism : TraitFunction
{

    /// <summary>
    ///     List of entries to add (or remove) from the metabolizer types of the organ.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public HashSet<ProtoId<MetabolizerTypePrototype>> Types { get; private set; } = new();

    /// <summary>
    ///     List of metabolizer groups this should affect.
    ///     If empty, this will affect all metabolizer groups.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public HashSet<ProtoId<MetabolismGroupPrototype>>? Groups { get; private set; } = new();

    /// <summary>
    ///     If true, add these metabolizer types to the organ's metabolizer types.
    ///     Otherwise, remove them.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public bool Add = false;

    public override void OnPlayerSpawn(EntityUid uid,
        IComponentFactory factory,
        IEntityManager entityManager,
        ISerializationManager serializationManager)
    {
        if (!entityManager.TryGetComponent<BodyComponent>(uid, out var body))
            return;

        var bodySystem = entityManager.System<BodySystem>();

        if (bodySystem is null)
            return;

        if (!bodySystem.TryGetBodyOrganComponents<MetabolizerComponent>(uid, out var metabolizers, body))
            return;

        foreach (var (metabolizer, _) in metabolizers)
        {
            if (metabolizer.MetabolizerTypes is null
                || metabolizer.MetabolismGroups is null)
                continue;
            if (Groups == null || Groups.Count == 0)
                ApplyCulinaryAdaptation(metabolizer);
            // otherwise, if the metabolizer has any of the groups that the culinary adaptation metabolizer has, apply it
            else if (metabolizer.MetabolismGroups.Any(metabolismGroup => Groups.Contains(metabolismGroup.Id)))
                ApplyCulinaryAdaptation(metabolizer);
        }
    }

    /// <summary>
    ///     Apply the CulinaryAdaptation metabolizer type(s) this affected metabolizer!
    /// </summary>
    private void ApplyCulinaryAdaptation(MetabolizerComponent metabolizer)
    {
        foreach (var metabType in Types)
        {
            if (Add)
                metabolizer.MetabolizerTypes?.Add(metabType);
            else
                metabolizer.MetabolizerTypes?.Remove(metabType);
        }
    }
}
// FLOOF END

// Scales/modifies the size of the character using the Floofstation modified heightAdjustSystem function SetScale
public sealed partial class TraitSetScale : TraitFunction 
{
    [DataField]
    public float scale;
    
    public override void OnPlayerSpawn(EntityUid uid,
        IComponentFactory factory,
        IEntityManager entityManager,
        ISerializationManager serializationManager)
    { 
        entityManager.System<HeightAdjustSystem>().SetScale(uid, scale, restricted: false);
    }
}
