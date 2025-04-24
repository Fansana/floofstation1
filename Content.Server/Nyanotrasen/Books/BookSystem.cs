using Content.Shared.Interaction;
using Content.Shared.Books;
using Content.Shared.Verbs; // Floof - M3739 - #607 - Much of the following was spliced from Flooftier's code regarding
using Robust.Shared.Player; // Floof - M3739 - #607 - hyperlink books. The cherry-pick did not work well out of the box.

namespace Content.Server.Books
{
    public sealed class BookSystem : EntitySystem // Floof - M3739 - #607
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HyperlinkBookComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<HyperlinkBookComponent, GetVerbsEvent<AlternativeVerb>>(AddAltVerb); // Floof - M3739 - #607
        }

        private void OnActivate(EntityUid uid, HyperlinkBookComponent component, ActivateInWorldEvent args)
        {
            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;
// Begin Floof
            OpenURL(actor.PlayerSession, component.URL);
        }

        private void AddAltVerb(EntityUid uid, HyperlinkBookComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    OpenURL(actor.PlayerSession, component.URL);
                },
                Text = Loc.GetString("book-read-verb"),
                Priority = -2
            };
            args.Verbs.Add(verb);
        }

        public void OpenURL(ICommonSession session, string url)
        {
// End Floof
            var ev = new OpenURLEvent(url);
            RaiseNetworkEvent(ev, session.Channel);
        }
    }
}
