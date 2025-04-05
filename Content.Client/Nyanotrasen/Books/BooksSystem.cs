using Content.Shared.Books;
// Floof - M3739 - #607 - Content.Client.Links used to be here. Not anymore.
using Robust.Client.UserInterface;

namespace Content.Client.Books
{
    public sealed class BooksSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<OpenURLEvent>(OnOpenURL);
        }

        private void OnOpenURL(OpenURLEvent args)
        {
            var uriOpener = IoCManager.Resolve<IUriOpener>();
            uriOpener.OpenUri(args.URL);
        }
    }
}
