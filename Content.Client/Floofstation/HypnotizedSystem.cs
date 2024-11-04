using Content.Shared.Floofstation.Hypno;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using Robust.Client.Player;
using Content.Client.Overlays;

namespace Content.Client.Floofstation;

public sealed class HypnotizedSystem : EquipmentHudSystem<HypnotizedComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PsionicHypnoComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, PsionicHypnoComponent component, ref GetStatusIconsEvent args)
    {
        if (!IsActive || args.InContainer)
            return;

        if (_playerManager.LocalEntity is not { Valid: true } player
            || !TryComp<HypnotizedComponent>(player, out var hypnoComp)
            || hypnoComp.Master != uid)
            return;

        if (_prototype.TryIndex<StatusIconPrototype>(component.MasterIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
