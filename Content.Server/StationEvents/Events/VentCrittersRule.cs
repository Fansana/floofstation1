using Content.Server.StationEvents.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Storage;
using Content.Shared.Tools.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class VentCrittersRule : StationEventSystem<VentCrittersRuleComponent>
{
    /*
     * DO NOT COPY PASTE THIS TO MAKE YOUR MOB EVENT.
     * USE THE PROTOTYPE.
     */

    protected override void Started(EntityUid uid, VentCrittersRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var station))
        {
            return;
        }

        var locations = EntityQueryEnumerator<VentCritterSpawnLocationComponent, TransformComponent>();
        var validLocations = new List<EntityCoordinates>();
        while (locations.MoveNext(out var ventUid, out _, out var transform))
        {
            // Floof: do not spawn on welded vents
            if (TryComp<WeldableComponent>(ventUid, out var weldable) && weldable.IsWelded)
                continue;

            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == station)
            {
                validLocations.Add(transform.Coordinates);
                foreach (var spawn in EntitySpawnCollection.GetSpawns(component.Entries, RobustRandom))
                {
                    SpawnCritter(spawn, transform.Coordinates, component); // Floof - changed to delayed spawn
                }
            }
        }

        if (component.SpecialEntries.Count == 0 || validLocations.Count == 0)
        {
            return;
        }

        // guaranteed spawn
        var specialEntry = RobustRandom.Pick(component.SpecialEntries);
        var specialSpawn = RobustRandom.Pick(validLocations);
        SpawnCritter(specialEntry.PrototypeId, specialSpawn, component); // Floof - changed to delayed spawn

        foreach (var location in validLocations)
        {
            foreach (var spawn in EntitySpawnCollection.GetSpawns(component.SpecialEntries, RobustRandom))
            {
                SpawnCritter(spawn, location, component); // Floof - changed to delayed spawn
            }
        }
    }

    // Floof
    private void SpawnCritter(EntProtoId? protoId, EntityCoordinates coordinates, VentCrittersRuleComponent rule) =>
        DelayedSpawn(protoId, coordinates, rule.InitialDelay, rule.CrawlTime, rule.Popup, rule.Sound);
}
