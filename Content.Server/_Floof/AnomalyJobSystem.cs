using Content.Shared.Shadowkin;
using Content.Shared.Rejuvenate;

namespace Content.Server.FloofStation;

public sealed class AnomalyJobSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalyJobComponent, ComponentStartup>(OnInit);
    }

    private void OnInit(EntityUid uid, AnomalyJobComponent component, ComponentStartup args)
    {
        if (!TryComp<ShadowkinComponent>(uid, out var shadowkin))
            return;

        shadowkin.BlackeyeSpawn = false;
        RaiseLocalEvent(uid, new RejuvenateEvent());
    }
}
