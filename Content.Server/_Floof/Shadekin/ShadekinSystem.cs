using Content.Shared.Examine;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Humanoid;
using Content.Shared.Psionics;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared._Floof.Shadekin;
using Content.Shared.Rejuvenate;
using Content.Shared.Alert;
using Content.Shared.Rounding;
using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Content.Server.Abilities.Psionics;
using Content.Shared.Mobs;
using Content.Server._Floof;
using Content.Shared.Inventory;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Content.Server._Floof.Shadekin;

public sealed class ShadowkinSystem : EntitySystem
{
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;

    public const string ShadowkinSleepActionId = "ShadekinActionSleep";

    private const int MaxRandomTeleportAttempts = 20;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadekinComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<ShadekinComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<ShadekinComponent, EyeColorInitEvent>(OnEyeColorChange);
        SubscribeLocalEvent<ShadekinComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnInit(EntityUid uid, ShadekinComponent component, ComponentStartup args)
    {
        if (component.Blackeye)
            ApplyBlackEye(uid, component);

        _actionsSystem.AddAction(uid, ref component.ShadekinSleepAction, ShadowkinSleepActionId, uid);
    }

    private void OnEyeColorChange(EntityUid uid, ShadekinComponent component, EyeColorInitEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid)
            || !component.Blackeye // Floofstation
            || humanoid.EyeColor == component.OldEyeColor)
            return;

        component.OldEyeColor = humanoid.EyeColor;
        humanoid.EyeColor = component.BlackEyeColor;
        Dirty(uid, humanoid);
    }

    public void ApplyBlackEye(EntityUid uid, ShadekinComponent component)
    {
        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
        {
            component.OldEyeColor = humanoid.EyeColor;
            humanoid.EyeColor = component.BlackEyeColor;
            Dirty(uid, humanoid);
        }
    }

    private void OnRejuvenate(EntityUid uid, ShadekinComponent component, RejuvenateEvent args)
    {
        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
        {
            humanoid.EyeColor = component.OldEyeColor;
            Dirty(uid, humanoid);
        }
    }

    private void OnMobStateChanged(EntityUid uid, ShadekinComponent component, MobStateChangedEvent args)
    {
        if (HasComp<ShadekinCuffComponent>(uid))
            return;

        if (args.NewMobState == MobState.Critical || args.NewMobState == MobState.Dead)
        {
            if (TryComp<InventoryComponent>(uid, out var inventoryComponent) && _inventorySystem.TryGetSlots(uid, out var slots))
                foreach (var slot in slots)
                    _inventorySystem.TryUnequip(uid, slot.Name, true, true, false, inventoryComponent);

            SpawnAtPosition("ShadowkinShadow", Transform(uid).Coordinates);
            SpawnAtPosition("EffectFlashShadowkinDarkSwapOff", Transform(uid).Coordinates);

            var query = EntityQueryEnumerator<DarkHubComponent>();
            while (query.MoveNext(out var target, out var portal))
            {
                var coords = Transform(target).Coordinates;
                var newCoords = coords.Offset(_random.NextVector2(5));
                for (var i = 0; i < MaxRandomTeleportAttempts; i++)
                {
                    var randVector = _random.NextVector2(5);
                    newCoords = coords.Offset(randVector);
                    if (!_lookup.GetEntitiesIntersecting(newCoords.ToMap(EntityManager, _transform), LookupFlags.Static).Any())
                        break;
                }

                _joints.RecursiveClearJoints(uid);

                _transform.SetCoordinates(uid, newCoords);
                continue;
            }

            SpawnAtPosition("ShadowkinShadow", Transform(uid).Coordinates);
            SpawnAtPosition("EffectFlashShadowkinDarkSwapOn", Transform(uid).Coordinates);

            RaiseLocalEvent(uid, new RejuvenateEvent());
            if (TryComp<PsionicComponent>(uid, out var magic))
            {
                magic.Mana = 0;
                EnsureComp<ForcedSleepingComponent>(uid);
            }
        }
    }
}
