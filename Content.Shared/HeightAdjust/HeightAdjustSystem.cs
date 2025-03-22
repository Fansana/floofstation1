using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.HeightAdjust;

public sealed class HeightAdjustSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedContentEyeSystem _eye = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _appearance = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;


    /// <summary>
    ///     Changes the density of fixtures and zoom of eyes based on a provided float scale
    /// </summary>
    /// <param name="uid">The entity to modify values for</param>
    /// <param name="scale">The scale to multiply values by</param>
    /// <returns>True if all operations succeeded</returns>
    public bool SetScale(EntityUid uid, float scale, bool restricted = true) // Floofstation - added restricted flag
    {
        return SetScale(uid, new Vector2(scale, scale), restricted);
    }

    /// <summary>
    ///     Changes the density of fixtures and zoom of eyes based on a provided Vector2 scale
    /// </summary>
    /// <param name="uid">The entity to modify values for</param>
    /// <param name="scale">The scale to multiply values by</param>
    /// <returns>True if all operations succeeded</returns>
    public bool SetScale(EntityUid uid, Vector2 scale, bool restricted = true) // Floofstation - added restricted flag
    {
        var succeeded = true;
        var avg = (scale.X + scale.Y) / 2;

        if (_config.GetCVar(CCVars.HeightAdjustModifiesZoom) && EntityManager.TryGetComponent<ContentEyeComponent>(uid, out var eye))
            _eye.SetMaxZoom(uid, eye.MaxZoom * avg);
        else
            succeeded = false;

        if (_config.GetCVar(CCVars.HeightAdjustModifiesHitbox) && EntityManager.TryGetComponent<FixturesComponent>(uid, out var fixtures))
            foreach (var fixture in fixtures.Fixtures)
                _physics.SetRadius(uid, fixture.Key, fixture.Value, fixture.Value.Shape, MathF.MinMagnitude(fixture.Value.Shape.Radius * avg, 0.49f));
        else
            succeeded = false;

        if (restricted && EntityManager.HasComponent<HumanoidAppearanceComponent>(uid)) // Floofstation - added restricted check
            _appearance.SetScale(uid, scale);
        else if (EntityManager.HasComponent<HumanoidAppearanceComponent>(uid)) // Floofstation
            _appearance.SetScaleUnrestricted(uid, scale); // Floofstation
        else
            succeeded = false;

        RaiseLocalEvent(uid, new HeightAdjustedEvent { NewScale = scale });

        return succeeded;
    }
}
