using Content.Shared._Floof.Shadekin;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Content.Shared.Humanoid;
using Content.Shared.Abilities.Psionics;
using Content.Client.Overlays;

namespace Content.Client.Shadowkin;

public sealed partial class ShadowkinSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;

    private ColorTintOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadekinComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ShadekinComponent, ComponentShutdown>(Onhutdown);
        SubscribeLocalEvent<ShadekinComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ShadekinComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        Subs.CVar(_cfg, CCVars.NoVisionFilters, OnNoVisionFiltersChanged);

        _overlay = new();
    }

    private void OnInit(EntityUid uid, ShadekinComponent component, ComponentInit args)
    {
        if (uid != _playerMan.LocalEntity
            || _cfg.GetCVar(CCVars.NoVisionFilters))
            return;

        _overlayMan.AddOverlay(_overlay);
    }

    private void Onhutdown(EntityUid uid, ShadekinComponent component, ComponentShutdown args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(EntityUid uid, ShadekinComponent component, LocalPlayerAttachedEvent args)
    {
        if (_cfg.GetCVar(CCVars.NoVisionFilters))
            return;

        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, ShadekinComponent component, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnNoVisionFiltersChanged(bool enabled)
    {
        if (enabled)
            _overlayMan.RemoveOverlay(_overlay);
        else
            _overlayMan.AddOverlay(_overlay);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_cfg.GetCVar(CCVars.NoVisionFilters))
            return;

        var uid = _playerMan.LocalEntity;
        if (uid == null
            || !TryComp<ShadekinComponent>(uid, out var comp)
            || !TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        // 1/3 = 0.333...
        // intensity = min + (power / max)
        // intensity = intensity / 0.333
        // intensity = clamp intensity min, max

        var tintIntensity = 0.65f;
        if (!comp.Blackeye)
        {
            var min = 0.45f;
            var max = 0.75f;
            tintIntensity = Math.Clamp(min + (comp.Energy / comp.MaxEnergy) * 0.333f, min, max);
        }

        UpdateShader(new Vector3(humanoid.EyeColor.R, humanoid.EyeColor.G, humanoid.EyeColor.B), tintIntensity);
    }

    private void UpdateShader(Vector3? color, float? intensity)
    {
        while (_overlayMan.HasOverlay<ColorTintOverlay>())
            _overlayMan.RemoveOverlay(_overlay);

        if (color != null)
            _overlay.TintColor = color;
        if (intensity != null)
            _overlay.TintAmount = intensity;

        if (!_cfg.GetCVar(CCVars.NoVisionFilters))
            _overlayMan.AddOverlay(_overlay);
    }
}
