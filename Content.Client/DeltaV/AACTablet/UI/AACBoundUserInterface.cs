using Content.Shared.DeltaV.AACTablet; // Floof - M3739 - DeltaV namespace
using Content.Shared.DeltaV.QuickPhrase; // Floof - M3739 - DeltaV namespace
// using Robust.Client.UserInterface; | Floof - M3739 - This relies on a version of Robust Toolbox we don't have. 3/31/2025
using Robust.Shared.Prototypes;

namespace Content.Client.DeltaV.AACTablet.UI;

public sealed class AACBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private AACWindow? _window;

    public AACBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window?.Close();
        _window = new AACWindow(); // Floof - M3739 - CreateWindow relies on a version of Robust toolbox we don't have. 3/31/2025
        _window.OpenCentered(); // Floof - M3739 - That being said, we have to rely on the way we do this.
        _window.PhraseButtonPressed += OnPhraseButtonPressed;
        _window.OnClose += Close; // Floof
    }

    private void OnPhraseButtonPressed(List<ProtoId<QuickPhrasePrototype>> phraseId)
    {
        SendMessage(new AACTabletSendPhraseMessage(phraseId));
    }

    protected override void Dispose(bool disposing) // Floof
    {
        base.Dispose(disposing);
        _window?.Orphan();
    }
}
