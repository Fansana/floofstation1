using Content.Shared._DV.Damage.Components;
using Content.Shared.Damage.Events;

namespace Content.Shared._DV.Damage.Systems;

public sealed partial class BonusStaminaDamageSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BonusStaminaDamageComponent, TakeStaminaDamageEvent>(OnStamHit); // Floof - StaminaMeleeHitEvent replaced with TakeStaminaDamageEvent
    }

    private void OnStamHit(Entity<BonusStaminaDamageComponent> ent, ref TakeStaminaDamageEvent args) // Floof - StaminaMeleeHitEvent replaced with TakeStaminaDamageEvent
    {
        args.Multiplier *= ent.Comp.Multiplier;
    }
}
