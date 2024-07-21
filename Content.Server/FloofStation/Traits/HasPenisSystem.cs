using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.FloofStation.Traits.Events;
using Robust.Shared.Timing;
using JetBrains.Annotations;

namespace Content.Server.FloofStation.Traits;

[UsedImplicitly]
public sealed class HasPenisSystem : EntitySystem
{
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HasPenisComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<HasPenisComponent, GetVerbsEvent<AlternativeVerb>>(AddCumVerb);
        SubscribeLocalEvent<HasPenisComponent, CummingDoAfterEvent>(OnDoAfter);
    }

    //From UdderSystem.cs
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HasPenisComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var penis))
        {
            if (now < penis.NextGrowth)
                continue;

            penis.NextGrowth = now + penis.GrowthDelay;

            if (_mobState.IsDead(uid))
                continue;

            // Actually there is food digestion so no problem with instant reagent generation "OnFeed"
            if (EntityManager.TryGetComponent(uid, out HungerComponent? hunger))
            {
                // Is there enough nutrition to produce reagent?
                if (_hunger.GetHungerThreshold(hunger) < HungerThreshold.Okay)
                    continue;

                _hunger.ModifyHunger(uid, -penis.HungerUsage, hunger);
            }

            if (!_solutionContainer.ResolveSolution(uid, penis.SolutionName, ref penis.Solution))
                continue;

            _solutionContainer.TryAddReagent(penis.Solution.Value, penis.ReagentId, penis.QuantityPerUpdate, out _);
        }
    }

    private void AttemptCum(Entity<HasPenisComponent?> penis, EntityUid userUid, EntityUid containerUid)
    {
        if (!Resolve(penis, ref penis.Comp))
            return;

        var doargs = new DoAfterArgs(EntityManager, userUid, 5, new CummingDoAfterEvent(), penis, penis, used: containerUid)
        {
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            MovementThreshold = 1.0f,
        };

        _doAfterSystem.TryStartDoAfter(doargs);
    }

    private void OnDoAfter(Entity<HasPenisComponent> entity, ref CummingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Used == null)
            return;

        if (!_solutionContainer.ResolveSolution(entity.Owner, entity.Comp.SolutionName, ref entity.Comp.Solution, out var solution))
            return;

        if (!_solutionContainer.TryGetRefillableSolution(args.Args.Used.Value, out var targetSoln, out var targetSolution))
            return;

        args.Handled = true;
        var quantity = solution.Volume;
        if (quantity == 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("cum-verb-dry"), entity.Owner, args.Args.User);
            return;
        }

        if (quantity > targetSolution.AvailableVolume)
            quantity = targetSolution.AvailableVolume;

        var split = _solutionContainer.SplitSolution(entity.Comp.Solution.Value, quantity);
        _solutionContainer.TryAddSolution(targetSoln.Value, split);

        _popupSystem.PopupEntity(Loc.GetString("cum-verb-success", ("amount", quantity), ("target", Identity.Entity(args.Args.Used.Value, EntityManager))), entity.Owner,
            args.Args.User, PopupType.Medium);
    }

    //Based on BloodstreamSystem.cs
    private void OnComponentInit(Entity<HasPenisComponent> entity, ref ComponentInit args)
    {
        var cumSolution = _solutionContainer.EnsureSolution(entity.Owner, entity.Comp.SolutionName);

        cumSolution.MaxVolume = entity.Comp.CumMaxVolume;

        // Fill penis solution with CUM
        cumSolution.AddReagent(entity.Comp.ReagentId, entity.Comp.CumMaxVolume - cumSolution.Volume);
    }

    //Based on UdderSystem.cs
    /*TO-DO/Design choices:
     * Check for suit (-loincloth) to prevent action?
     * Be able to cum on the ground w/o a container.
     * -Add a check for a container in the active hand? Yes = fill container. No = Splooge on the ground.
     * Better text for actions. (Says you/your instead of the person.)
     */
    public void AddCumVerb(Entity<HasPenisComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.Using == null ||
             !args.CanInteract ||
             !EntityManager.HasComponent<RefillableSolutionComponent>(args.Using.Value)) //see if removing this part lets you cum on the ground?
            return;

        var cumContainer = entity.Comp.Solution;
        var uid = entity.Owner;
        var user = args.User;
        var used = args.Using.Value;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                AttemptCum(uid, user, used);
            },
            Text = Loc.GetString("cum-verb-get-text"),
            Priority = 2
        };
        args.Verbs.Add(verb);
    }
}
