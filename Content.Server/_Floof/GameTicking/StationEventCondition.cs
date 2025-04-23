using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.StationEvents;
using Content.Server.StationEvents.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.FloofStation.GameTicking;

/// <summary>
///     Represents an abstract condition required for a station event to be chosen from the random event pool.
/// </summary>
/// <remarks>
///     Implementations should avoid performing expensive checks.
///     Any data that may be expensive to compute should instead be precomputed and stored in <see cref="Dependencies"/>
/// </remarks>
[Serializable, ImplicitDataDefinitionForInheritors]
public abstract partial class StationEventCondition
{
    /// <summary>
    ///     If true, the event will only be run if this condition is NOT met.
    /// </summary>
    [DataField]
    public bool Inverted = false;

    public abstract bool IsMet(EntityPrototype proto, StationEventComponent component, Dependencies dependencies);

    /// <summary>
    ///     Entity system and other dependencies used by station event conditions.
    ///     GameTicker allocates an instance of this before passing it to all events.
    /// </summary>
    public sealed class Dependencies(IEntityManager entMan, GameTicker ticker, EventManagerSystem eventManager)
    {
        public ISawmill Log = Logger.GetSawmill("station-event-conditions");

        public IEntityManager EntMan => entMan;
        public GameTicker Ticker => ticker;
        public EventManagerSystem EventManager => eventManager;

        public MindSystem Minds = default!;
        public SharedIdCardSystem IdCard = default!;

        [Dependency] public IPrototypeManager ProtoMan = default!;
        [Dependency] public IRobustRandom Random = default!;
        [Dependency] public IPlayerManager PlayerManager = default!;

        /// <summary>
        ///     The list of all players along with their jobs.
        /// </summary>
        public List<(ICommonSession session, EntityUid uid, ProtoId<JobPrototype> job)> Players = new();
        public Dictionary<ProtoId<JobPrototype>, int> JobCounts = new();
        public Dictionary<ProtoId<DepartmentPrototype>, int> DeptCounts = new();

        // Lookups
        private readonly Dictionary<string, ProtoId<JobPrototype>> _jobTitleToPrototype = new();
        private readonly Dictionary<ProtoId<JobPrototype>, List<ProtoId<DepartmentPrototype>>> _jobToDepts = new();

        /// <summary>
        ///     Called once after the instantiation of the class.
        /// </summary>
        public void Initialize()
        {
            IoCManager.InjectDependencies(this);

            // We cannot use entity system dependencies outside of ESC context.
            IdCard = EntMan.System<SharedIdCardSystem>();
            Minds = EntMan.System<MindSystem>();

            // Build the inverse lookups - SharedJobSystem contains methods that iterate over all of those lists each time,
            // Resulting in an O(n^2 * m) performance cost for each update() call.
            foreach (var job in ProtoMan.EnumeratePrototypes<JobPrototype>())
            {
                _jobTitleToPrototype[job.LocalizedName] = job.ID;

                var depts = ProtoMan.EnumeratePrototypes<DepartmentPrototype>()
                    .Where(it => it.Roles.Contains(job.ID))
                    .Select(it => new ProtoId<DepartmentPrototype>(it.ID))
                    .ToList();

                _jobToDepts[job.ID] = depts;
            }
        }

        /// <summary>
        ///     Called once shortly before passing this object to IsMet() to collect the necessary data about the round.
        /// </summary>
        public void Update()
        {
            JobCounts.Clear();
            DeptCounts.Clear();

            // Collect data about the jobs of the players in the round
            Players.Clear();
            foreach (var session in PlayerManager.Sessions)
            {
                if (session.AttachedEntity is not {} player
                    || session.Status is SessionStatus.Zombie or SessionStatus.Disconnected
                    || !Minds.TryGetMind(session, out var mind, out var mindComponent))
                    continue;

                ProtoId<JobPrototype> job = default;
                // 1: Try to get the job from the ID the person holds
                if (IdCard.TryFindIdCard(player, out var idCard) && idCard.Comp.JobTitle is {} jobTitle)
                    _jobTitleToPrototype.TryGetValue(jobTitle, out job);

                // 2: If failed, try to fetch it from the mind component instead
                if (job == default
                    && EntMan.TryGetComponent<JobComponent>(mind, out var jobComp)
                    && jobComp.Prototype is {} mindJobProto
                )
                    job = mindJobProto;

                // If both have failed, skip the player
                if (job == default)
                    continue;

                // Update the info
                Players.Add((session, player, job));
                JobCounts[job] = JobCounts.GetValueOrDefault(job, 0) + 1;
                // Increment the number  of players in each dept this job belongs to
                if (_jobToDepts.TryGetValue(job, out var depts))
                {
                    foreach (var dept in depts)
                        DeptCounts[dept] = DeptCounts.GetValueOrDefault(dept, 0) + 1;
                }
            }

            #if DEBUG
                Log.Debug($"Event conditions data: Job counts: {string.Join(", ", JobCounts)}");
                Log.Debug($"Dept counts: {string.Join(", ", DeptCounts)}");
            #endif
        }
    }
}
