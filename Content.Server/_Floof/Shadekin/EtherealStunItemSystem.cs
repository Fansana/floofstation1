using Content.Shared.Interaction.Events;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared._Floof.Shadekin;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Stacks;

namespace Content.Server._Floof.Shadekin;

public sealed class EtherealStunItemSystem : EntitySystem
{
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStackSystem _sharedStackSystem = default!;
    [Dependency] private readonly ShadowkinSystem _shadowkinSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<EtherealStunItemComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(EntityUid uid, EtherealStunItemComponent component, UseInHandEvent args)
    {
        foreach (var ent in _lookup.GetEntitiesInRange(uid, component.Radius))
        {
            if (!TryComp<EtherealComponent>(ent, out var ethereal)
                || !ethereal.CanBeStunned)
                continue;

            RemComp(ent, ethereal);

            if (TryComp<StaminaComponent>(ent, out var stamina))
                _stamina.TakeStaminaDamage(ent, stamina.CritThreshold, stamina, ent);

            if (TryComp<ShadekinComponent>(ent, out var shadekin))
            {
                shadekin.Energy = 0;
                _shadowkinSystem.UpdateAlert(ent, shadekin);

                var lightQuery = _lookup.GetEntitiesInRange(uid, 5, flags: LookupFlags.StaticSundries)
                    .Where(x => HasComp<PoweredLightComponent>(x));
                foreach (var light in lightQuery)
                    _ghost.DoGhostBooEvent(light);

                var effect = SpawnAtPosition("ShadekinPhaseIn2Effect", Transform(uid).Coordinates);
                Transform(effect).LocalRotation = Transform(uid).LocalRotation;
            }
            else
                SpawnAtPosition("ShadekinShadow", Transform(uid).Coordinates);
        }

        if (!component.DeleteOnUse)
            return;

        if (TryComp<StackComponent>(uid, out var stack))
            _sharedStackSystem.Use(uid, 1, stack);
        else
            QueueDel(uid);
    }
}
