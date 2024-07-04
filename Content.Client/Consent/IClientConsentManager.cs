using Content.Shared.Consent;

namespace Content.Client.Consent;

public interface IClientConsentManager
{
    event Action OnServerDataLoaded;
    bool HasLoaded { get; }

    void Initialize();
    void UpdateConsent(PlayerConsentSettings consentSettings);
    PlayerConsentSettings GetConsent();
}
