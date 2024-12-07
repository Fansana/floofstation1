using Content.Server.Actions;
using Content.Server.Polymorph.Components;
using Content.Shared.Actions;
using Content.Shared.Inventory.Events;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Polymorph.Systems;

public sealed partial class PolymorphSystem
{
    [Dependency] private readonly IEntityManager _entities = default!;
    private void InitializeProvider()
    {
        SubscribeLocalEvent<PolymorphProviderComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<PolymorphProviderComponent, GotUnequippedEvent>(OnUnEquipped);

    }

    private void OnUnEquipped(EntityUid uid, PolymorphProviderComponent component, ref GotUnequippedEvent args)
    {
        var polymorphable = _entities.EnsureComponent<PolymorphableComponent>(args.Equipee);
        RemovePolymorphAction(component.Polymorph, (args.Equipee, polymorphable));
    }

    private void OnEquipped(EntityUid uid, PolymorphProviderComponent component, ref GotEquippedEvent args)
    {

        var polymorphable = _entities.EnsureComponent<PolymorphableComponent>(args.Equipee);
        CreatePolymorphAction(component.Polymorph, (args.Equipee, polymorphable));
    }

}
