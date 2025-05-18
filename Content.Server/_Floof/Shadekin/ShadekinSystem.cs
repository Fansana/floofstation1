using Content.Shared.Humanoid;
using Content.Shared.Bed.Sleep;
using Content.Shared._Floof.Shadekin;
using Content.Shared.Rejuvenate;
using Content.Shared.Alert;
using Content.Shared.Rounding;
using Content.Shared.Actions;
using Content.Shared.Mood;
using Content.Shared.Mobs;
using Content.Shared.Inventory;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using System.Linq;
using Microsoft.CodeAnalysis;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Content.Shared.Examine;
using Content.Server.Ghost;
using Content.Server.Light.Components;


namespace Content.Server._Floof.Shadekin;

public sealed class ShadowkinSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;

    public const string ShadowkinPhaseActionId = "ShadekinActionPhase";
    public const string ShadowkinSleepActionId = "ShadekinActionSleep";
    private const int MaxRandomTeleportAttempts = 20;

    private sealed class LightCone
    {
        public float Direction { get; set; }
        public float InnerWidth { get; set; }
        public float OuterWidth { get; set; }
    }
    private readonly Dictionary<string, List<LightCone>> lightMasks = new()
    {
        ["/Textures/Effects/LightMasks/cone.png"] = new List<LightCone>
    {
        new LightCone { Direction = 0, InnerWidth = 30, OuterWidth = 60 }
    },
        ["/Textures/Effects/LightMasks/double_cone.png"] = new List<LightCone>
    {
        new LightCone { Direction = 0, InnerWidth = 30, OuterWidth = 60 },
        new LightCone { Direction = 180, InnerWidth = 30, OuterWidth = 60 }
    }
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadekinComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<ShadekinComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<ShadekinComponent, EyeColorInitEvent>(OnEyeColorChange);
        SubscribeLocalEvent<ShadekinComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ShadekinComponent, ShadekinPhaseActionEvent>(OnPhaseAction);
    }

    private void OnInit(EntityUid uid, ShadekinComponent component, ComponentStartup args)
    {
        if (component.Blackeye)
            ApplyBlackEye(uid, component);
        else
            _actionsSystem.AddAction(uid, ref component.ShadekinPhaseAction, ShadowkinPhaseActionId, uid);

        _actionsSystem.AddAction(uid, ref component.ShadekinSleepAction, ShadowkinSleepActionId, uid);
        UpdateAlert(uid, component);
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

        component.Energy = 0;

        UpdateAlert(uid, component);
    }

    public void UpdateAlert(EntityUid uid, ShadekinComponent component)
    {
        var lightseverity = component.LightExposure;
        var energyseverity = (short) ContentHelpers.RoundToLevels(component.Energy, component.MaxEnergy, 5);

        if (component.Blackeye)
            energyseverity = 0;

        _alerts.ShowAlert(uid, "Shadekin-" + lightseverity + "-" + energyseverity);
    }

    private void OnRejuvenate(EntityUid uid, ShadekinComponent component, RejuvenateEvent args)
    {
        if (component.Blackeye)
            return;

        component.Energy = component.MaxEnergy;

        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
        {
            humanoid.EyeColor = component.OldEyeColor;
            Dirty(uid, humanoid);
        }

        _actionsSystem.AddAction(uid, ref component.ShadekinPhaseAction, ShadowkinPhaseActionId, uid);
        UpdateAlert(uid, component);
    }

    private void OnPhaseAction(EntityUid uid, ShadekinComponent component, ShadekinPhaseActionEvent args)
    {
        if (component.LightExposure == 4)
        {
            _popup.PopupEntity(Loc.GetString("shadekin-lightextreme-energy"), uid, uid, PopupType.LargeCaution);
            args.Handled = true;
            return;
        }

        var price = 0;
        if (component.LightExposure == 3)
            price += 50;
        else if (component.LightExposure == 2)
            price += 30;
        else if (component.LightExposure == 1)
            price += 15;

        if (HasComp<EtherealComponent>(uid))
        {
            Phase(uid);
            args.Handled = true;
            return;
        }

        price += 100;

        if (component.Energy >= price)
        {
            component.Energy -= price;
            Phase(uid);
            UpdateAlert(uid, component);
        }
        else
            _popup.PopupEntity(Loc.GetString("shadekin-no-energy"), uid, uid, PopupType.LargeCaution);

        args.Handled = true;
    }

    public void Phase(EntityUid uid)
    {
        if (TryComp<EtherealComponent>(uid, out var ethereal))
        {
            var tileref = Transform(uid).Coordinates.GetTileRef();
            if (tileref != null
            && _physics.GetEntitiesIntersectingBody(uid, (int) CollisionGroup.Impassable).Count > 0)
            {
                _popup.PopupEntity(Loc.GetString("revenant-in-solid"), uid, uid);
                return;
            }

            // TODO: Phase blocker map zone.
            // TODO: Cannot phase while in container...

            if (HasComp<ShadekinComponent>(uid))
            {
                var lightQuery = _lookup.GetEntitiesInRange(uid, 5, flags: LookupFlags.StaticSundries)
                    .Where(x => HasComp<PoweredLightComponent>(x));
                foreach (var light in lightQuery)
                    _ghost.DoGhostBooEvent(light);

                var effect = SpawnAtPosition("ShadekinPhaseInEffect", Transform(uid).Coordinates);
                Transform(effect).LocalRotation = Transform(uid).LocalRotation;
            }
            else
                SpawnAtPosition("ShadekinShadow", Transform(uid).Coordinates);

            RemComp(uid, ethereal);
        }
        else
        {
            EnsureComp<EtherealComponent>(uid);

            if (HasComp<ShadekinComponent>(uid))
            {
                var lightQuery = _lookup.GetEntitiesInRange(uid, 5, flags: LookupFlags.StaticSundries)
                    .Where(x => HasComp<PoweredLightComponent>(x));
                foreach (var light in lightQuery)
                    _ghost.DoGhostBooEvent(light);

                var effect = SpawnAtPosition("ShadekinPhaseOutEffect", Transform(uid).Coordinates);
                Transform(effect).LocalRotation = Transform(uid).LocalRotation;
            }
            else
                SpawnAtPosition("ShadekinShadow", Transform(uid).Coordinates);
        }
    }

    private void OnMobStateChanged(EntityUid uid, ShadekinComponent component, MobStateChangedEvent args)
    {
        if (component.Blackeye
            || HasComp<ShadekinCuffComponent>(uid))
            return;

        if (args.NewMobState == MobState.Dead)
        {
            if (TryComp<InventoryComponent>(uid, out var inventoryComponent) && _inventorySystem.TryGetSlots(uid, out var slots))
                foreach (var slot in slots)
                    _inventorySystem.TryUnequip(uid, slot.Name, true, true, false, inventoryComponent);

            SpawnAtPosition("ShadekinShadow", Transform(uid).Coordinates);

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

            var effect = SpawnAtPosition("ShadekinPhaseIn2Effect", Transform(uid).Coordinates);
            Transform(effect).LocalRotation = Transform(uid).LocalRotation;

            RaiseLocalEvent(uid, new RejuvenateEvent());
            component.Energy = 0;
            EnsureComp<ForcedSleepingComponent>(uid);
        }
    }

    private Angle GetAngle(EntityUid lightUid, SharedPointLightComponent lightComp, EntityUid targetUid)
    {
        var (lightPos, lightRot) = _transform.GetWorldPositionRotation(lightUid);
        lightPos += lightRot.RotateVec(lightComp.Offset);

        var (targetPos, targetRot) = _transform.GetWorldPositionRotation(targetUid);

        var mapDiff = targetPos - lightPos;

        var oppositeMapDiff = (-lightRot).RotateVec(mapDiff);
        var angle = oppositeMapDiff.ToWorldAngle();

        if (angle == double.NaN && _transform.ContainsEntity(targetUid, lightUid) || _transform.ContainsEntity(lightUid, targetUid))
        {
            angle = 0f;
        }

        return angle;
    }

    public float GetLightExposure(EntityUid uid)
    {
        var illumination = 0f;

        var lightQuery = _lookup.GetEntitiesInRange(uid, 20)
                .Where(x => HasComp<PointLightComponent>(x));

        foreach (var light in lightQuery)
        {
            if (!TryComp<PointLightComponent>(light, out var pointLight))
                continue;

            if (!pointLight.Enabled
                || pointLight.Radius < 1
                || pointLight.Energy <= 0)
                continue;

            var (lightPos, lightRot) = _transform.GetWorldPositionRotation(light);
            lightPos += lightRot.RotateVec(pointLight.Offset);

            if (!_examine.InRangeUnOccluded(light, uid, pointLight.Radius, null, false))
                continue;

            Transform(uid).Coordinates.TryDistance(EntityManager, Transform(light).Coordinates, out var dist);

            var denom = dist / pointLight.Radius;
            var attenuation = 1 - (denom * denom);
            var calculatedLight = 0f;

            if (pointLight.MaskPath is not null)
            {
                var angleToTarget = GetAngle(light, pointLight, uid);
                foreach (var cone in lightMasks[pointLight.MaskPath])
                {
                    var coneLight = 0f;
                    var angleAttenuation = (float) Math.Min((float) Math.Max(cone.OuterWidth - angleToTarget, 0f), cone.InnerWidth) / cone.OuterWidth;

                    if (angleToTarget.Degrees - cone.Direction > cone.OuterWidth)
                        continue;
                    else if (angleToTarget.Degrees - cone.Direction > cone.InnerWidth
                        && angleToTarget.Degrees - cone.Direction < cone.OuterWidth)
                        coneLight = pointLight.Energy * attenuation * attenuation * angleAttenuation;
                    else
                        coneLight = pointLight.Energy * attenuation * attenuation;

                    calculatedLight = Math.Max(calculatedLight, coneLight);
                }
            }
            else
                calculatedLight = pointLight.Energy * attenuation * attenuation;

            illumination += calculatedLight; //Math.Max(illumination, calculatedLight);
        }

        return illumination;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ShadekinComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            component.Accumulator += frameTime;

            if (_mobState.IsDead(uid))
                continue;

            if (component.Accumulator <= 1)
                continue;

            component.Accumulator -= 1;
            var ethereal = HasComp<EtherealComponent>(uid);

            var lightExposure = GetLightExposure(uid);
            if (lightExposure >= 20f)
            {
                component.LightExposure = 4;
                if (!ethereal)
                    RaiseLocalEvent(uid, new MoodEffectEvent("ShadekinLightExtreme"));
            }
            else if (lightExposure >= 10f)
            {
                component.LightExposure = 3;
                if (!ethereal)
                    RaiseLocalEvent(uid, new MoodEffectEvent("ShadekinLightHigh"));
            }
            else if (lightExposure >= 5f)
            {
                component.LightExposure = 2;
                if (!ethereal)
                    RaiseLocalEvent(uid, new MoodEffectEvent("ShadekinLightAnnoyed"));
            }
            else if (lightExposure >= 0.8f)
                component.LightExposure = 1;
            else
            {
                component.LightExposure = 0;
                if (!ethereal)
                    RaiseLocalEvent(uid, new MoodEffectEvent("ShadekinDarkness"));
            }

            UpdateAlert(uid, component);

            if (component.Blackeye
                || HasComp<ShadekinCuffComponent>(uid))
                continue;

            if (component.Energy > component.MaxEnergy)
                component.Energy = component.MaxEnergy;

            if (component.Energy < 0)
                component.Energy = 0;

            if (component.Energy < component.MaxEnergy)
            {
                var energyGain = 1f;

                if (!ethereal)
                {
                    if (component.LightExposure == 4)
                        energyGain = 0f;
                    else if (component.LightExposure == 3)
                        energyGain = 0.1f;
                    else if (component.LightExposure == 2)
                        energyGain = 0.4f;
                    else if (component.LightExposure == 1)
                        energyGain = 0.5f;
                }

                if (HasComp<SleepingComponent>(uid))
                    energyGain *= 2;

                energyGain *= component.Energymultiplier;

                component.Energy += energyGain;
            }

            UpdateAlert(uid, component);
        }
    }
}

