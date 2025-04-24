using System.Linq;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;


namespace Content.Shared.FloofStation.Clothing.ClothingLimit;


public sealed class ClothingLimitSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    private EntityQuery<ClothingLimitComponent> _limitQuery;

    public override void Initialize()
    {
        _limitQuery = GetEntityQuery<ClothingLimitComponent>();

        SubscribeLocalEvent<ClothingLimitComponent, BeingEquippedAttemptEvent>(OnTryEquip);
    }

    private void OnTryEquip(Entity<ClothingLimitComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        ClothingComponent? equippingClothing = default!;
        if (args.Cancelled
            || !Resolve(ent, ref equippingClothing, logMissing: true) // We want this to log errors so it would fail tests if an entity has this comp but not clothing
            || !ent.Comp.CheckNonEquipped && !equippingClothing.Slots.HasFlag(args.SlotFlags)
            || ent.Comp.LimitGroups.Count <= 0)
            return;

        // Collect all clothing on the entity, find clothing that has a ClothingLimitComponent with intersecting groups, count them
        var equippingGroups = new Dictionary<string, int>(ent.Comp.LimitGroups.Count);
        var enumerator = _inventory.GetSlotEnumerator(args.EquipTarget);

        while (enumerator.MoveNext(out var equipped))
        {
            if (equipped.ContainedEntity is not { } equippedClothing ||
                !_limitQuery.TryComp(equippedClothing, out var equippedLimit))
                continue;

            // Increment the number of items in each intersecting group
            foreach (var group in ent.Comp.LimitGroups.Intersect(equippedLimit.LimitGroups))
                equippingGroups[group] = equippingGroups.GetValueOrDefault(group, 0) + 1;

            // Check limits
            foreach (var count in equippingGroups.Values)
            {
                if (count < ent.Comp.MaxCount)
                    continue;

                args.Reason = "clothing-limit-exceeded";
                args.Cancel();
                return;
            }
        }

    }
}
