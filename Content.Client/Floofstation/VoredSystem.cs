using Content.Shared.FloofStation;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Client.Floofstation;

public sealed partial class VoredSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoredComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<VoredComponent, ComponentShutdown>(Onhutdown);
        SubscribeLocalEvent<VoredComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<VoredComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnInit(EntityUid uid, VoredComponent component, ComponentInit args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        component.Stream = _audio.PlayGlobal(component.SoundBelly, Filter.Local(), false)?.Entity;
    }

    private void Onhutdown(EntityUid uid, VoredComponent component, ComponentShutdown args)
    {
        if (uid != _playerMan.LocalEntity)
            return;

        QueueDel(component.Stream);
    }

    private void OnPlayerAttached(EntityUid uid, VoredComponent component, LocalPlayerAttachedEvent args)
    {
        component.Stream = _audio.PlayGlobal(component.SoundBelly, Filter.Local(), false)?.Entity;
    }

    private void OnPlayerDetached(EntityUid uid, VoredComponent component, LocalPlayerDetachedEvent args)
    {
        QueueDel(component.Stream);
    }
}
