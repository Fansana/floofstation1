using Content.Shared._Floof.Shadekin;
using Content.Shared.Inventory.Events;
using Content.Shared.Clothing.Components;
using Content.Shared.Actions;
using Content.Shared.Popups;

namespace Content.Server._Floof.Shadekin;

public sealed class EtherealPhaseSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ShadekinSystem _shadekinSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EtherealPhaseComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<EtherealPhaseComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EtherealPhaseComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<EtherealPhaseComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<EtherealPhaseComponent, ShadekinPhaseActionEvent>(OnPhaseAction);
    }

    private void OnInit(EntityUid uid, EtherealPhaseComponent component, MapInitEvent args)
    {
        Toggle(uid, component, true);
    }

    public void OnShutdown(EntityUid uid, EtherealPhaseComponent component, ComponentShutdown args)
    {
        Toggle(uid, component, false);
    }

    private void OnEquipped(EntityUid uid, EtherealPhaseComponent component, GotEquippedEvent args)
    {
        if (!TryComp<ClothingComponent>(uid, out var clothing)
            || !clothing.Slots.HasFlag(args.SlotFlags))
            return;

        EnsureComp<EtherealPhaseComponent>(args.Equipee);
    }

    private void OnUnequipped(EntityUid uid, EtherealPhaseComponent component, GotUnequippedEvent args)
    {
        RemComp<EtherealPhaseComponent>(args.Equipee);
    }

    private void OnPhaseAction(EntityUid uid, EtherealPhaseComponent component, ShadekinPhaseActionEvent args)
    {
        if (HasComp<ShadekinCuffComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString("phase-fail-generic"), uid, uid, PopupType.LargeCaution);
            args.Handled = true;
            return;
        }

        if (TryComp<ShadekinComponent>(uid, out var shadekin) && !shadekin.Blackeye)
        {
            args.Handled = true;
            return;
        }

        _shadekinSystem.Phase(uid);
        args.Handled = true;
    }

    private void Toggle(EntityUid uid, EtherealPhaseComponent component, bool toggle)
    {
        if (toggle)
            _actionsSystem.AddAction(uid, ref component.ShadekinPhaseAction, "ShadekinActionPhase", uid);
        else
            _actionsSystem.RemoveAction(uid, component.ShadekinPhaseAction);
    }
}
