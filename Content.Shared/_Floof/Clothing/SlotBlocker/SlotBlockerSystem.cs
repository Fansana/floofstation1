using System.Diagnostics.CodeAnalysis;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;


namespace Content.Shared._Floof.Clothing.SlotBlocker;


public sealed class SlotBlockerSystem : EntitySystem
{
    public static SlotFlags IgnoredSlots = SlotFlags.POCKET;

    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private EntityQuery<SlotBlockerComponent> _blockerQuery = default!;
    private EntityQuery<ClothingComponent> _clothingQuery = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SlotBlockerComponent, MapInitEvent>(OnMapInitSanityCheck);
        SubscribeLocalEvent<InventorySlotBlockingComponent, IsEquippingAttemptEvent>(OnCheckSlotBlockingEquip);
        SubscribeLocalEvent<InventorySlotBlockingComponent, IsUnequippingAttemptEvent>(OnCheckSlotBlockingUnequip);
        SubscribeLocalEvent<SlotBlockerComponent, BeingEquippedAttemptEvent>(OnCheckBlockedEquip);
        SubscribeLocalEvent<SlotBlockerComponent, BeingUnequippedAttemptEvent>(OnCheckBlockedUnequip);

        _blockerQuery = GetEntityQuery<SlotBlockerComponent>();
        _clothingQuery = GetEntityQuery<ClothingComponent>();
    }

    private void OnMapInitSanityCheck(Entity<SlotBlockerComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<ClothingComponent>(ent, out var clothing))
        {
            Log.Warning("SlotBlockerComponent applied to entity without ClothingComponent: " + ToPrettyString(ent));
            return;
        }

        // Tempting to check if the blocker can block its own unequip, but there's just too much things to consider. Will just do a runtime check.
    }

    private void OnCheckSlotBlockingEquip(Entity<InventorySlotBlockingComponent> ent, ref IsEquippingAttemptEvent args)
    {
        if (args.Cancelled
            || _blockerQuery.HasComp(ent) // This will be handled in OnCheckBlockedEquip
            || !TryComp<InventoryComponent>(args.EquipTarget, out var inventory)
            || !IsSlotObstructed((ent, inventory), null, CheckType.Equip, args.SlotFlags, out var reason))
            return;

        args.Cancel();
        args.Reason = reason;
    }

    private void OnCheckSlotBlockingUnequip(Entity<InventorySlotBlockingComponent> ent, ref IsUnequippingAttemptEvent args)
    {
        if (args.Cancelled
            || _blockerQuery.HasComp(ent) // This will be handled in OnCheckBlockedUnequip
            || !TryComp<InventoryComponent>(args.UnEquipTarget, out var inventory)
            || !IsSlotObstructed((ent, inventory), null, CheckType.Unequip, args.SlotFlags, out var reason))
            return;

        args.Cancel();
        args.Reason = reason;
    }

    private void OnCheckBlockedEquip(Entity<SlotBlockerComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (args.Cancelled
            || !TryComp<InventoryComponent>(args.EquipTarget, out var inventory)
            || !IsSlotObstructed((ent, inventory), ent, CheckType.Equip, args.SlotFlags, out var reason))
            return;

        args.Cancel();
        args.Reason = reason;
    }

    private void OnCheckBlockedUnequip(Entity<SlotBlockerComponent> ent, ref BeingUnequippedAttemptEvent args)
    {
        if (args.Cancelled
            || !TryComp<InventoryComponent>(args.UnEquipTarget, out var inventory)
            || !IsSlotObstructed((ent, inventory), ent, CheckType.Unequip, args.SlotFlags, out var reason))
            return;

        args.Cancel();
        args.Reason = reason;
    }

    /// <summary>
    ///     Checks whether a slot (or any of the slots) is blocked.
    /// </summary>
    /// <param name="ent">Entity to check for blocking clothing.</param>
    /// <param name="equipment">Optional. If present, will check the "blocked by" clothing as well.</param>
    /// <param name="check">Check type to perform (equip, unequip)</param>
    /// <param name="targetSlot">Slot into which the equipment will/is equipped.</param>
    /// <param name="reason"></param>
    /// <returns></returns>
    public bool IsSlotObstructed(
        Entity<InventoryComponent> ent,
        Entity<SlotBlockerComponent>? equipment,
        CheckType check,
        SlotFlags targetSlot,
        out string? reason)
    {
        reason = null;
        if (equipment?.Comp.IgnoreOtherBlockers == true)
            return false;

        var slots = ent.Comp.Slots;
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            var container = ent.Comp.Containers[i];
            if ((slot.SlotFlags & IgnoredSlots) != 0
                || container.ContainedEntity is not { Valid: true } other
                || other == equipment?.Owner // An item cannot block its own removal
            )
                continue;

            // Check whether this clothing is blocked by this slot
            if (equipment is {} equipment2
                && BlockerObstructsSlot(other, other, ref equipment2.Comp.BlockedBy, check, slot.SlotFlags, targetSlot, ref reason))
                return true;

            // Check whether the clothing in this slot blocks this clothing
            if (_blockerQuery.TryComp(other, out var otherBlocker)
                && BlockerObstructsSlot(equipment, other, ref otherBlocker.Blocks, check, slot.SlotFlags, targetSlot, ref reason))
                return true;
        }

        return false;
    }


    private bool BlockerObstructsSlot(
        EntityUid? whitelistTarget,
        EntityUid blocker,
        ref BlockerDefinition blocks,
        CheckType check,
        SlotFlags blockerInSlot,
        SlotFlags equipmentInSlot,
        ref string? reason)
    {
        if (!blocks.Slots.HasFlag(equipmentInSlot)
            || (blocks.EnableInSlots & blockerInSlot) == 0
            // If there's an equipment whitelist, then equipment must be present to consider this blocker.
            || blocks.Whitelist != null && (whitelistTarget == null || _whitelist.IsWhitelistFail(blocks.Whitelist, whitelistTarget.Value))
            // Blacklist however always passes if there's no equipment.
            || blocks.Blacklist != null && whitelistTarget != null && _whitelist.IsBlacklistPass(blocks.Blacklist, whitelistTarget.Value)
        )
            return false;

        bool blocked;
        string reasonLoc;
        switch (check)
        {
            case CheckType.Equip:
                blocked = blocks.PreventsEquip;
                reasonLoc = "slot-blocker-blocked-equipped";
                break;
            case CheckType.Unequip:
                blocked = blocks.PreventsUnequip;
                reasonLoc = "slot-blocker-blocked-unequipped";
                break;
            default:
                blocked = true;
                reasonLoc = "slot-blocker-blocked-generic";
                break;
        }

        if (blocked)
            reason = Loc.GetString(reasonLoc, ("blocker", blocker));
        return blocked;
    }

    [Flags]
    public enum CheckType
    {
        Equip = 1,
        Unequip = 2,

        IgnoreBlockerPreference = 0
    }
}
