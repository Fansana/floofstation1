using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Floof.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Server.Nuke;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using System.Runtime.CompilerServices;

namespace Content.Server.Floof.Xenoarchaeology.XenoArtifacts.Effects.Systems;

/// <summary>
/// This handles <see cref="ActivateNukeArtifactComponent"/>
/// </summary>
public sealed class ActivateNukeArtifactSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MapSystem _map = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ActivateNukeArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnActivated(EntityUid uid, ActivateNukeArtifactComponent component, ArtifactActivatedEvent args)
    {

        foreach ()
        {
            nuke.
        }

    }
}
