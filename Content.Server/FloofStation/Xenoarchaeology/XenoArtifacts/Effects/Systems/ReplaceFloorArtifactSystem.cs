using System.Numerics;
using Content.Shared.Maps;
using Content.Server.Light.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Server.FloofStation.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Robust.Shared.Random;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.FloofStation.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class ReplaceFloorArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IEntityManager _manager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly ITileDefinitionManager _tiledef = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ReplaceFloorArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnActivated(EntityUid uid, ReplaceFloorArtifactComponent component, ArtifactActivatedEvent args)
    {
        if (!TryComp<ReplaceFloorArtifactComponent>(uid, out var _comp))
            return;

        if (component == null)
            return;

        var xform = Transform(uid);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var localpos = xform.Coordinates.Position;

        var tilerefs = _map.GetLocalTilesIntersecting(
            uid,
            grid,
            new Box2(localpos + new Vector2(-component.Range, -component.Range), localpos + new Vector2(component.Range, component.Range))
        );

        foreach (var tileref in tilerefs)
        {
            float distance = MathF.Sqrt(MathF.Pow(tileref.X - localpos.X, 2) + MathF.Pow(tileref.Y - localpos.Y, 2));

            if (_random.NextFloat() < (component.Falloff/distance))
            {
                var tile = _tiledef[component.Replacement];
                _map.SetTile(xform.GridUid.Value, grid, tileref.GridIndices, new Tile(tile.TileId));
            }
        }
        _audio.PlayPvs(component.PolySound, uid);
    }
}
