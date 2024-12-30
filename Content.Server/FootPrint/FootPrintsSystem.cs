using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.FootPrint;
using Content.Shared.Standing;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Forensics;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.FootPrint;

public sealed class FootPrintsSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IMapManager _map = default!;

    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!; // Floof

    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<MobThresholdsComponent> _mobThresholdQuery;
    private EntityQuery<AppearanceComponent> _appearanceQuery;
    private EntityQuery<LayingDownComponent> _layingQuery;

    public override void Initialize()
    {
        base.Initialize();

        _transformQuery = GetEntityQuery<TransformComponent>();
        _mobThresholdQuery = GetEntityQuery<MobThresholdsComponent>();
        _appearanceQuery = GetEntityQuery<AppearanceComponent>();
        _layingQuery = GetEntityQuery<LayingDownComponent>();

        SubscribeLocalEvent<FootPrintsComponent, ComponentStartup>(OnStartupComponent);
        SubscribeLocalEvent<FootPrintsComponent, MoveEvent>(OnMove);
    }

    private void OnStartupComponent(EntityUid uid, FootPrintsComponent component, ComponentStartup args)
    {
        component.StepSize = Math.Max(0f, component.StepSize + _random.NextFloat(-0.05f, 0.05f));
    }

    private void OnMove(EntityUid uid, FootPrintsComponent component, ref MoveEvent args)
    {
        // Floof: clear stored DNAs if footprints are now invisible
        if (component.PrintsColor.A <= .3f)
            component.DNAs.Clear();

        if (component.PrintsColor.A <= .3f // avoid creating footsteps that are invisible
            || TryComp<PhysicsComponent>(uid, out var physics) && physics.BodyStatus != BodyStatus.OnGround // Floof: do not create footprints if the entity is flying
            || !_transformQuery.TryComp(uid, out var transform)
            || !_mobThresholdQuery.TryComp(uid, out var mobThreshHolds)
            || !_map.TryFindGridAt(_transform.GetMapCoordinates((uid, transform)), out var gridUid, out _))
            return;

        // Floof - this is dumb
        // var dragging = mobThreshHolds.CurrentThresholdState is MobState.Critical or MobState.Dead
        //                || _layingQuery.TryComp(uid, out var laying) && laying.IsCrawlingUnder;
        var dragging = TryComp<StandingStateComponent>(uid, out var standing) && standing.CurrentState == StandingState.Lying; // Floof - replaced the above
        var distance = (transform.LocalPosition - component.StepPos).Length();
        var stepSize = dragging ? component.DragSize : component.StepSize;

        if (!(distance > stepSize))
            return;

        // Floof section
        var entities = _lookup.GetEntitiesIntersecting(uid, LookupFlags.All);
        foreach (var entityUid in entities.Where(entityUid => HasComp<PuddleFootPrintsComponent>(entityUid)))
            return; // are we on a puddle? we exit, ideally we would exchange liquid and DNA with the puddle but meh, too lazy to do that now.
        // Floof section end

        component.RightStep = !component.RightStep;

        var entity = Spawn(component.StepProtoId, CalcCoords(gridUid, component, transform, dragging));
        var footPrintComponent = EnsureComp<FootPrintComponent>(entity);

        // Floof section
        var forensics = EntityManager.EnsureComponent<ForensicsComponent>(entity);
        if (TryComp<ForensicsComponent>(uid, out var ownerForensics)) // transfer owner DNA into the footsteps
            forensics.DNAs.UnionWith(ownerForensics.DNAs);
        // Floof section end

        footPrintComponent.PrintOwner = uid;
        Dirty(entity, footPrintComponent);

        if (_appearanceQuery.TryComp(entity, out var appearance))
        {
            _appearance.SetData(entity, FootPrintVisualState.State, PickState(uid, dragging), appearance);
            _appearance.SetData(entity, FootPrintVisualState.Color, component.PrintsColor, appearance);
        }

        if (!_transformQuery.TryComp(entity, out var stepTransform))
            return;

        stepTransform.LocalRotation = dragging
            ? (transform.LocalPosition - component.StepPos).ToAngle() + Angle.FromDegrees(-90f)
            : transform.LocalRotation + Angle.FromDegrees(180f);

        component.PrintsColor = component.PrintsColor.WithAlpha(Math.Max(0f, component.PrintsColor.A - component.ColorReduceAlpha));
        component.StepPos = transform.LocalPosition;

        if (!TryComp<SolutionContainerManagerComponent>(entity, out var solutionContainer)
            || !_solution.ResolveSolution((entity, solutionContainer), footPrintComponent.SolutionName, ref footPrintComponent.Solution, out var solution)
            || string.IsNullOrWhiteSpace(component.ReagentToTransfer) || solution.Volume >= 1)
            return;

        _solution.TryAddReagent(footPrintComponent.Solution.Value, component.ReagentToTransfer, 1, out _);
    }

    private EntityCoordinates CalcCoords(EntityUid uid, FootPrintsComponent component, TransformComponent transform, bool state)
    {
        if (state)
            return new EntityCoordinates(uid, transform.LocalPosition);

        var offset = component.RightStep
            ? new Angle(Angle.FromDegrees(180f) + transform.LocalRotation).RotateVec(component.OffsetPrint)
            : new Angle(transform.LocalRotation).RotateVec(component.OffsetPrint);

        return new EntityCoordinates(uid, transform.LocalPosition + offset);
    }

    private FootPrintVisuals PickState(EntityUid uid, bool dragging)
    {
        var state = FootPrintVisuals.BareFootPrint;

        if (_inventory.TryGetSlotEntity(uid, "shoes", out _))
            state = FootPrintVisuals.ShoesPrint;

        if (_inventory.TryGetSlotEntity(uid, "outerClothing", out var suit) && TryComp<PressureProtectionComponent>(suit, out _))
            state = FootPrintVisuals.SuitPrint;

        if (dragging)
            state = FootPrintVisuals.Dragging;

        return state;
    }
}
