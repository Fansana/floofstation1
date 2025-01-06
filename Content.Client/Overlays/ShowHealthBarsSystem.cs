using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Graphics;
using System.Linq;

namespace Content.Client.Overlays;

/// <summary>
/// Adds a health bar overlay.
/// </summary>
public sealed class ShowHealthBarsSystem : EquipmentHudSystem<ShowHealthBarsComponent>
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private EntityHealthBarOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShowHealthBarsComponent, AfterAutoHandleStateEvent>(OnHandleState);

        _overlay = new(EntityManager);
    }

    private void OnHandleState(Entity<ShowHealthBarsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOverlay(ent);
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ShowHealthBarsComponent> component)
    {
        base.UpdateInternal(component);

        foreach (var damageContainerId in component.Components.SelectMany(x => x.DamageContainers))
        {
            _overlay.DamageContainers.Add(damageContainerId);
        }

        if (!_overlayMan.HasOverlay<EntityHealthBarOverlay>())
        {
            _overlayMan.AddOverlay(_overlay);
        }
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlay.DamageContainers.Clear();
        _overlayMan.RemoveOverlay(_overlay);
    }
}
