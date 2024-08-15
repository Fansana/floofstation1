using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.Strip.Components;

namespace Content.Shared.Strip;

public abstract class SharedStrippableSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ThievingSystem _thieving = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StrippingComponent, CanDropTargetEvent>(OnCanDropOn);
        SubscribeLocalEvent<StrippableComponent, CanDropDraggedEvent>(OnCanDrop);
        SubscribeLocalEvent<StrippableComponent, DragDropDraggedEvent>(OnDragDrop);
    }

<<<<<<< HEAD
    public (TimeSpan Time, ThievingStealth Stealth) GetStripTimeModifiers(EntityUid user, EntityUid target, TimeSpan initialTime)
=======
    public (float Time, bool Stealth) GetStripTimeModifiers(EntityUid user, EntityUid target, float initialTime)
>>>>>>> parent of febd6c735c (Merge pull request #6 from VMSolidus/latest-experimental-psychics)
    {
        var userEv = new BeforeStripEvent(initialTime);
        RaiseLocalEvent(user, userEv);
        var ev = new BeforeGettingStrippedEvent(userEv.Time, userEv.Stealth);
        RaiseLocalEvent(target, ev);
        return (ev.Time, ev.Stealth);
    }

    private void OnDragDrop(EntityUid uid, StrippableComponent component, ref DragDropDraggedEvent args)
    {
        // If the user drags a strippable thing onto themselves.
        if (args.Handled || args.Target != args.User)
            return;

        StartOpeningStripper(args.User, (uid, component));
        args.Handled = true;
    }

    public virtual void StartOpeningStripper(EntityUid user, Entity<StrippableComponent> component, bool openInCombat = false)
    {

    }

    private void OnCanDropOn(EntityUid uid, StrippingComponent component, ref CanDropTargetEvent args)
    {
        args.Handled = true;
        args.CanDrop |= uid == args.User &&
                        HasComp<StrippableComponent>(args.Dragged) &&
                        HasComp<HandsComponent>(args.User) &&
                        HasComp<StrippingComponent>(args.User);
    }

    private void OnCanDrop(EntityUid uid, StrippableComponent component, ref CanDropDraggedEvent args)
    {
        args.CanDrop |= args.Target == args.User &&
                        HasComp<StrippingComponent>(args.User) &&
                        HasComp<HandsComponent>(args.User);

        if (args.CanDrop)
            args.Handled = true;
    }

    public void StripPopup(string messageId, ThievingStealth stealth, EntityUid target, EntityUid? user = null, EntityUid? item = null, string slot = "")
    {
        bool subtle = stealth == ThievingStealth.Subtle;
        PopupType? popupSize = _thieving.GetPopupTypeFromStealth(stealth);

        if (popupSize.HasValue) // We should always have a value if we're not hidden
            _popup.PopupEntity(Loc.GetString(messageId,
            ("user", subtle ? Loc.GetString("thieving-component-user") : user ?? EntityUid.Invalid),
            ("item", subtle ? Loc.GetString("thieving-component-item") : item ?? EntityUid.Invalid),
            ("slot", slot)),
            target, target, popupSize.Value);
    }
}
