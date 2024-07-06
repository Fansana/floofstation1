using Content.Client.FurryServers;
using JetBrains.Annotations;
using Robust.Client.State;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.ResourceManagement;

namespace Content.Client.UserInterface.Systems.EscapeMenu;

[UsedImplicitly]
public sealed class FurryServersUIController : UIController
{
    private FurryServersWindow _furryServersWindow = default!;

    public void OpenWindow()
    {
        EnsureWindow();

        _furryServersWindow.OpenCentered();
        _furryServersWindow.MoveToFront();
    }

    private void EnsureWindow()
    {
        if (_furryServersWindow is { Disposed: false })
            return;

        _furryServersWindow = UIManager.CreateWindow<FurryServersWindow>();
    }

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_furryServersWindow.IsOpen)
        {
            _furryServersWindow.Close();
        }
        else
        {
            OpenWindow();
        }
    }
}
