using Content.Shared.Doors;
using Content.Shared.Emag.Components;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Robust.Shared.Network;


namespace Content.Shared._Floof.Lock;


/// <summary>
///     Prevents locked doors from being opened.
/// </summary>
public sealed class DoorLockSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LockComponent, BeforeDoorOpenedEvent>(OnDoorOpenAttempt);
    }

    private void OnDoorOpenAttempt(Entity<LockComponent> ent, ref BeforeDoorOpenedEvent args)
    {
        if (!ent.Comp.Locked || ent.Comp.BreakOnEmag && HasComp<EmaggedComponent>(ent))
            return;

        args.Cancel();
        if (args.User is {} user && _net.IsServer)
            _popup.PopupCursor(Loc.GetString("entity-storage-component-locked-message"), user);
    }
}
