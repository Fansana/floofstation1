using Content.Shared.CCVar;
using Content.Shared.FloofStation;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client._Floof;

public sealed partial class VoredSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoredComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<VoredComponent, ComponentShutdown>(Onhutdown);
        SubscribeLocalEvent<VoredComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<VoredComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        Subs.CVar(_configManager, FloofCCVars.VoreSoundEnabled, VoreSoundCVarChanged);
    }

    private void OnInit(EntityUid uid, VoredComponent component, ComponentInit args)
    {
        if (uid != _playerMan.LocalEntity
            || !_configManager.GetCVar(FloofCCVars.VoreSoundEnabled))
            return;

        component.Stream = _audio.PlayGlobal(component.SoundBelly, Filter.Local(), false)?.Entity;
    }

    private void Onhutdown(EntityUid uid, VoredComponent component, ComponentShutdown args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        QueueDel(component.Stream);
    }

    private void VoreSoundCVarChanged(bool voreEnabled)
    {
        if (!TryComp<VoredComponent>(_playerMan.LocalEntity, out var component))
            return;

        if (voreEnabled)
            component.Stream = _audio.PlayGlobal(component.SoundBelly, Filter.Local(), false)?.Entity;
        else
            QueueDel(component.Stream);
    }

    private void OnPlayerAttached(EntityUid uid, VoredComponent component, LocalPlayerAttachedEvent args)
    {
        if (!_configManager.GetCVar(FloofCCVars.VoreSoundEnabled))
            return;

        component.Stream = _audio.PlayGlobal(component.SoundBelly, Filter.Local(), false)?.Entity;
    }

    private void OnPlayerDetached(EntityUid uid, VoredComponent component, LocalPlayerDetachedEvent args)
    {
        QueueDel(component.Stream);
    }
}
