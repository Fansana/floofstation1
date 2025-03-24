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
    public bool SetScale(EntityUid uid, float scale, bool restricted = true) // Floofstation - added restricted flag, only set false if you know what you're doing!
    {
        return SetScale(uid, new Vector2(scale, scale), restricted: restricted); // Floofstation - added restricted flag
    }

    /// <summary>
    ///     Changes the density of fixtures and zoom of eyes based on a provided Vector2 scale
    /// </summary>
    /// <param name="uid">The entity to modify values for</param>
    /// <param name="scale">The scale to multiply values by</param>
    /// <returns>True if all operations succeeded</returns>
    public bool SetScale(EntityUid uid, Vector2 scale, bool restricted = true) // Floofstation - added restricted flag, only set false if you know what you're doing!
    {
        
        // Floofstation start - get current player size and adjust based on the scale
        var appearance = EntityManager.EnsureComponent<HumanoidAppearanceComponent>(uid);
        float height = appearance.Height * scale.Y;
        float width = appearance.Width * scale.X;
        var adjScale = new Vector2(height, width);
        // Floofstation end
        
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
        
        if (EntityManager.HasComponent<HumanoidAppearanceComponent>(uid))
            _appearance.SetScale(uid, adjScale, restricted: restricted); // Floofstation - added restricted flag and switched to using adjusted scale
        else
            succeeded = false;

        RaiseLocalEvent(uid, new HeightAdjustedEvent { NewScale = scale });

        return succeeded;
    }
}
