using Content.Shared.Rejuvenate;
using Content.Shared._Floof.Shadekin;

namespace Content.Server._Floof;

public sealed class AnomalyJobSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalyJobComponent, ComponentStartup>(OnInit);
    }

    private void OnInit(EntityUid uid, AnomalyJobComponent component, ComponentStartup args)
    {
        if (!TryComp<ShadekinComponent>(uid, out var shadowkin))
            return;

        shadowkin.Blackeye = false;
        RaiseLocalEvent(uid, new RejuvenateEvent());
    }
}
