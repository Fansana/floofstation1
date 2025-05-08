using Content.Shared.Abilities.Psionics;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Popups;
using Content.Server.Chat.Systems;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Actions.Events;
using Content.Shared.Smoking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;

namespace Content.Server.Abilities.Psionics
{
    public sealed class PyrokinesisPowerSystem : EntitySystem
    {
        [Dependency] private readonly FlammableSystem _flammableSystem = default!;
        [Dependency] private readonly SharedPsionicAbilitiesSystem _psionics = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly SmokingSystem _smoking = default!;
        [Dependency] private readonly ChatSystem _chat = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PyrokinesisPowerActionEvent>(OnPowerUsed);
        }
        private void OnPowerUsed(PyrokinesisPowerActionEvent args)
        {
            if (!_psionics.OnAttemptPowerUse(args.Performer, "pyrokinesis"))
                return;

            // Light your ciggy with style if possible (before considering lighting yourself on fire.)
            if (
                args.Performer == args.Target &&
                _inventory.TryGetSlotEntity(args.Target, "mask", out var item) &&
                TryComp<SmokableComponent>(item, out var smokableComponentSelf) &&
                smokableComponentSelf.State == SmokableState.Unlit
            )
            {
                _smoking.SetSmokableState(item.Value, SmokableState.Lit, smokableComponentSelf);
                var freeHand = _hands.TryGetEmptyHand(args.Target, out _);
                var locString = freeHand ?
                    "pyrokinesis-power-used-smokable-self" : "pyrokinesis-power-used-smokable-self-no-hands";
                if (freeHand)
                    _chat.TryEmoteWithChat(args.Performer, "Snap");
                _popupSystem.PopupEntity(Loc.GetString(locString, ("who", args.Performer), ("target", item)), args.Target, Shared.Popups.PopupType.Small);
                args.Handled = true;
            }
            else if (
                TryComp<SmokableComponent>(args.Target, out var smokableComponentOther) &&
                smokableComponentOther.State == SmokableState.Unlit
            )
            {
                _smoking.SetSmokableState(args.Target, SmokableState.Lit, smokableComponentOther);
                _popupSystem.PopupEntity(Loc.GetString("pyrokinesis-power-used-smokable", ("target", args.Target)), args.Target, Shared.Popups.PopupType.LargeCaution);
                args.Handled = true;
            }
            else if (TryComp<FlammableComponent>(args.Target, out var flammableComponent))
            {
                flammableComponent.FireStacks += 5;
                _flammableSystem.Ignite(args.Target, args.Target);
                _popupSystem.PopupEntity(Loc.GetString("pyrokinesis-power-used", ("target", args.Target)), args.Target, Shared.Popups.PopupType.LargeCaution);
                args.Handled = true;
            }
            if (args.Handled)
                _psionics.LogPowerUsed(args.Performer, "pyrokinesis");
        }
    }
}
