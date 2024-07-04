using Content.Shared.Consent;
using Robust.Shared.Network;

namespace Content.Client.Consent;

public sealed class ClientConsentManager : IClientConsentManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;

    // TODO: sync all players consent settings with ServerConsentManager, for client prediction
    private PlayerConsentSettings? _consent;

    public bool HasLoaded => _consent is not null;

    public event Action? OnServerDataLoaded;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgUpdateConsent>(HandleUpdateConsent);
    }

    public void UpdateConsent(PlayerConsentSettings consentSettings)
    {
        var msg = new MsgUpdateConsent
        {
            Consent = consentSettings
        };
        _netManager.ClientSendMessage(msg);
    }

    public PlayerConsentSettings GetConsent()
    {
        if (_consent is null)
            throw new InvalidOperationException("Consent settings not loaded yet?");

        return _consent;
    }

    private void HandleUpdateConsent(MsgUpdateConsent message)
    {
        _consent = message.Consent;

        OnServerDataLoaded?.Invoke();
    }
}
