using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Server.Consent;

namespace Content.Server.FloofStation.Horny;

public sealed class MindbrokenSystem : EntitySystem
{
    [Dependency] private readonly ConsentSystem _consent = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HornyComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, HornyComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;
        if (!_consent.HasConsent(args.Examined, "Horny"))
            return;

        var identity = Identity.Entity(args.Examined, EntityManager);
        args.PushMarkup($"[color=pink]{Loc.GetString("consent-Horny-examine", ("user", identity))}[/color]");
    }
}













