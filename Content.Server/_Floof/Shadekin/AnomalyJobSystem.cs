using Content.Shared.Rejuvenate;
using Content.Shared._Floof.Shadekin;
using Robust.Shared.Random;
using Robust.Shared.Physics.Systems;

namespace Content.Server._Floof.Shadekin;

public sealed class AnomalyJobSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalyJobComponent, ComponentStartup>(OnInit);
    }

    private void OnInit(EntityUid uid, AnomalyJobComponent component, ComponentStartup args)
    {
        var spawns = new List<Entity<AnomalyJobSpawnComponent>>();
        var query = EntityQueryEnumerator<AnomalyJobSpawnComponent>();
        while (query.MoveNext(out var spawnUid, out var spawn))
        {
            spawns.Add((spawnUid, spawn));
        }

        _random.Shuffle(spawns);

        foreach (var (spawnUid, spawn) in spawns)
        {
            _joints.RecursiveClearJoints(uid);
            _transform.SetCoordinates(uid, Transform(spawnUid).Coordinates);
            break;
        }

        var effect = SpawnAtPosition("ShadekinPhaseIn2Effect", Transform(uid).Coordinates);
        Transform(effect).LocalRotation = Transform(uid).LocalRotation;

        if (!TryComp<ShadekinComponent>(uid, out var shadowkin))
            return;

        shadowkin.Blackeye = false;
        RaiseLocalEvent(uid, new RejuvenateEvent());
    }
}
