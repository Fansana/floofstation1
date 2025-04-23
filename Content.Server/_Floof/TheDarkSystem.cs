using Content.Server.GameTicking.Events;
using Content.Server.Shuttles.Components;
using Content.Shared.Shuttles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.FloofStation;

public sealed class TheDarkSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HideoutGeneratorComponent, MapInitEvent>(SetupTheDark);
        SubscribeLocalEvent<HideoutGeneratorComponent, ComponentShutdown>(DestroyTheDark);
    }

    private void SetupTheDark(EntityUid uid, HideoutGeneratorComponent component, MapInitEvent args)
    {
        Logger.Debug(uid.ToString());
        var mapId = _mapManager.CreateMap();
        _mapManager.AddUninitializedMap(mapId);

        #if DEBUG
        // I do not want to deal with this. The dark spawns in every single integration test case,
        // slows down our test suite, causes random test failures, and more. If you want to test the dark,
        // compile the server in the "Tools" configuration. - Mnemotechnician
        return;
        #endif

        if (!_loader.TryLoad(mapId, "/Maps/Floof/hideout.yml", out var uids))
            return;

        foreach (var id in uids)
        {
            EnsureComp<PreventPilotComponent>(id);
        }
        component.Generated.Add(mapId);
        _mapManager.DoMapInitialize(mapId);
    }

    private void DestroyTheDark(EntityUid uid, HideoutGeneratorComponent component, ComponentShutdown args)
    {

        foreach (var mapId in component.Generated)
        {
            if (!_mapManager.MapExists(mapId))
                continue;

            _mapManager.DeleteMap(mapId);
        }


    }
}
