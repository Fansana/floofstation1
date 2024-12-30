using System.Diagnostics.CodeAnalysis;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Linq;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FootPrint;


namespace Content.Server.Chemistry.TileReactions;

/// <summary>
/// Turns all of the reagents on a puddle into water.
/// </summary>
[DataDefinition]
public sealed partial class CleanTileReaction : ITileReaction
{
    /// <summary>
    /// How much it costs to clean 1 unit of reagent.
    /// </summary>
    /// <remarks>
    /// In terms of space cleaner can clean 1 average puddle per 5 units.
    /// </remarks>
    [DataField("cleanCost")]
    public float CleanAmountMultiplier { get; private set; } = 0.25f;

    /// <summary>
    /// What reagent to replace the tile conents with.
    /// </summary>
    [DataField("reagent", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string ReplacementReagent = "Water";

    FixedPoint2 ITileReaction.TileReact(TileRef tile, ReagentPrototype reagent, FixedPoint2 reactVolume)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var entities = entMan.System<EntityLookupSystem>().GetLocalEntitiesIntersecting(tile, 0f).ToArray();
        var puddleQuery = entMan.GetEntityQuery<PuddleComponent>();
        var solutionContainerSystem = entMan.System<SolutionContainerSystem>();
        // Multiply as the amount we can actually purge is higher than the react amount.
        var purgeAmount = reactVolume / CleanAmountMultiplier;

        foreach (var entity in entities)
        {
            // Floof - separated this into a separate function to incroporate cleaning footprints
            if (!TryGetCleanableSolution(entity, entMan, solutionContainerSystem, out var puddleSolution))
                continue;

            var purgeable = solutionContainerSystem.SplitSolutionWithout(puddleSolution.Value, purgeAmount, ReplacementReagent, reagent.ID);

            purgeAmount -= purgeable.Volume;

            solutionContainerSystem.TryAddSolution(puddleSolution.Value, new Solution(ReplacementReagent, purgeable.Volume));

            if (purgeable.Volume <= FixedPoint2.Zero)
                break;
        }

        return (reactVolume / CleanAmountMultiplier - purgeAmount) * CleanAmountMultiplier;
    }

    // Floof
    private bool TryGetCleanableSolution(
        EntityUid entity,
        IEntityManager entMan,
        SharedSolutionContainerSystem solutionContainerSystem,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solution)
    {
        solution = default;
        if (entMan.TryGetComponent<PuddleComponent>(entity, out var puddle) &&
            solutionContainerSystem.TryGetSolution(entity, puddle.SolutionName, out var puddleSolution, out _))
        {
            solution = puddleSolution;
            return true;
        }

        if (entMan.TryGetComponent<FootPrintComponent>(entity, out var footPrint) &&
            solutionContainerSystem.TryGetSolution(entity, footPrint.SolutionName, out var footPrintSolution, out _))
        {
            solution = footPrintSolution;
            return true;
        }

        return false;
    }
}
