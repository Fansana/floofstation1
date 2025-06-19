using Content.Shared.FloofStation.Info;


namespace Content.Client._Floof.Info;


public sealed class NsfwDisclaimerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeNetworkEvent<ShowNsfwPopupDisclaimerMessage>(OnShowPopup);
    }

    private void OnShowPopup(ShowNsfwPopupDisclaimerMessage message)
    {
        var window = new NsfwDisclaimerWindow();
        window.OpenCentered();
    }
}
