using Content.Shared.Inventory.Events;
using Content.Shared.Clothing.Components;

namespace Content.Server._Floof.Shadekin;

public sealed class ShadekinCuffSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadekinCuffComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ShadekinCuffComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(EntityUid uid, ShadekinCuffComponent component, GotEquippedEvent args)
    {
        if (!TryComp<ClothingComponent>(uid, out var clothing)
            || !clothing.Slots.HasFlag(args.SlotFlags))
            return;

        EnsureComp<ShadekinCuffComponent>(args.Equipee);
    }

    private void OnUnequipped(EntityUid uid, ShadekinCuffComponent component, GotUnequippedEvent args)
    {
        RemComp<ShadekinCuffComponent>(args.Equipee);
    }
}
