using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.Customization.Systems;
using Content.Shared.Players;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Server.Traits;

public sealed class TraitSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly CharacterRequirementsSystem _characterRequirements = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    // When the player is spawned in, add all trait components selected during character creation
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Begin Floof: Sort traits by priority so we can specify some traits that may be reliant on other traits to properly function
        var sortedTraits = new List<TraitPrototype>();

        foreach (var traitId in args.Profile.TraitPreferences)
        {
            if (_prototype.TryIndex<TraitPrototype>(traitId, out var traitPrototype))
            {
                sortedTraits.Add(traitPrototype);
            }
            else
            {
                DebugTools.Assert($"No trait found with ID {traitId}!");
                return;
            }
        }

        sortedTraits.Sort();

        foreach (var traitPrototype in sortedTraits)
        {
            // Moved converting to prototypes to above loop in order to sort before applying them. End Floof modifications.
            if (!_characterRequirements.CheckRequirementsValid(
                traitPrototype.Requirements,
                _prototype.Index<JobPrototype>(args.JobId ?? _prototype.EnumeratePrototypes<JobPrototype>().First().ID),
                args.Profile, _playTimeTracking.GetTrackerTimes(args.Player), args.Player.ContentData()?.Whitelisted ?? false, traitPrototype,
                EntityManager, _prototype, _configuration,
                out _))
                continue;

            AddTrait(args.Mob, traitPrototype);
        }
    }

    /// <summary>
    ///     Adds a single Trait Prototype to an Entity.
    /// </summary>
    public void AddTrait(EntityUid uid, TraitPrototype traitPrototype)
    {
        foreach (var function in traitPrototype.Functions)
            function.OnPlayerSpawn(uid, _componentFactory, EntityManager, _serialization);
    }
}
