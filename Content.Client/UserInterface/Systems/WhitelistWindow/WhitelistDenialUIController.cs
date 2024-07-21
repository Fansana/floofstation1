using Content.Client.Options.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Console;
using Content.Client.UserInterface.Systems.WhitelistWindow.Controls;

namespace Content.Client.UserInterface.Systems.WhitelistWindow;

[UsedImplicitly]
public sealed class WhitelistDenialUIController : UIController
{
    public override void Initialize()
    {
    }
    private WhitelistDenialWindow _whitelistDenialWindow = default!;

    private void EnsureWindow()
    {
        if (_whitelistDenialWindow is { Disposed: false })
            return;

        _whitelistDenialWindow = UIManager.CreateWindow<WhitelistDenialWindow>();
    }

    public void OpenWindow(string denialMessage)
    {
        EnsureWindow();

        _whitelistDenialWindow.SetDenialMessage(denialMessage);

        _whitelistDenialWindow.OpenCentered();
        _whitelistDenialWindow.MoveToFront();
    }
}
