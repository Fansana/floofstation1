using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Robust.Shared.Network;

namespace Content.Shared.Consent;

public abstract partial class SharedConsentSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MindContainerComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(Entity<MindContainerComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (_mindSystem.GetMind(ent, ent) is not { } mind
            || !TryComp<MindComponent>(mind, out var mindComponent)
            || mindComponent.UserId is not { } userId)
        {
            return;
        }

        var user = args.User;

        args.Verbs.Add(new()
        {
            Text = Loc.GetString("consent-examine-verb"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () =>
            {
                var message = GetConsentText(userId);
                _examineSystem.SendExamineTooltip(user, ent, message, getVerbs: false, centerAtCursor: false);
            },
            Category = VerbCategory.Examine,
            CloseMenu = true,
        });
    }

    protected virtual FormattedMessage GetConsentText(NetUserId userId)
    {
        return new FormattedMessage();
    }
}
