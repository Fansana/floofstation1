using Robust.Shared.Random;
using Robust.Shared.Audio.Systems;
using Content.Server.Light.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Server.FloofStation.Xenoarchaeology.XenoArtifacts.Effects.Components;

namespace Content.Server.FloofStation.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class NightclubArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IEntityManager _manager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<NightclubArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnActivated(EntityUid uid, NightclubArtifactComponent component, ArtifactActivatedEvent args)
    {
        if (!TryComp<NightclubArtifactComponent>(uid, out var _comp))
            return;

        if (component == null)
            return;

        if (component.Replacements.Count <= 0)
            return;

        var artifactXform = Transform(uid);
        var lights = new HashSet<Entity<PoweredLightComponent>>();
        _lookup.GetEntitiesInRange(artifactXform.Coordinates, component.Range, lights, LookupFlags.StaticSundries);

        foreach (var light in lights)
        {
            var xform = Transform(light);
            var coords = xform.Coordinates;
            var rot = xform.LocalRotation;

            _manager.QueueDeleteEntity(light); // delete the old light

            var replacement = _manager.SpawnAtPosition(_random.Pick(component.Replacements), coords); // spawn a new one
            Transform(replacement).LocalRotation = rot;

            _audio.PlayPvs(component.PolySound, replacement);
        }
    }
}
