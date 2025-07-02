using Content.Shared.Administration.Managers;
using Content.Shared.Consent;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;


namespace Content.Shared._Floof.Examine;


public abstract class SharedCustomExamineSystem : EntitySystem
{
    public static ProtoId<ConsentTogglePrototype> NsfwDescConsent = "NSFWDescriptions";
    public static int PublicMaxLength = 256, SubtleMaxLength = 256;
    /// <summary>Max length of any content field, INCLUDING markup.</summary>
    public static int AbsolutelyMaxLength = 1024;

    [Dependency] private readonly SharedConsentSystem _consent = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly ISharedAdminManager _admin = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CustomExamineComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<CustomExamineComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.PublicData.Content is null && ent.Comp.SubtleData.Content is null)
            return;

        var publicData = ent.Comp.PublicData;
        var subtleData = ent.Comp.SubtleData;
        using (args.PushGroup(nameof(CustomExamineComponent), -1))
        {
            var allowNsfw = _consent.HasConsent(args.Examiner, NsfwDescConsent);
            var showSubtle = subtleData.Content is not null
                && (!subtleData.RequiresConsent || allowNsfw)
                && _examine.InRangeUnOccluded(args.Examiner, args.Examined);
            // If subtle is shown, then is is guaranteed that InRangeUnOccluded is true for both cases - TrimData will ensure it
            var showPublic = publicData.Content is not null
                && (!publicData.RequiresConsent || allowNsfw)
                && (showSubtle || _examine.InRangeUnOccluded(args.Examiner, args.Examined));

            // Theoretically these should be trimmed by the server while assigning the message
            if (showPublic)
                args.PushMarkup(publicData.Content!);

            if (showSubtle)
                args.PushMarkup(subtleData.Content!);

            // If something is hidden due to consent preferences, add a note
            if (!allowNsfw && (!showPublic || !showSubtle))
                args.PushMarkup(Loc.GetString("custom-examine-nsfw-hidden"));
        }
    }

    protected bool CanChangeExamine(ICommonSession actor, EntityUid examinee)
    {
        return actor.AttachedEntity == examinee || _admin.IsAdmin(actor);
    }

    protected void TrimData(ref CustomExamineData publicData, ref CustomExamineData subtleData)
    {
        TrimData(ref publicData);
        TrimData(ref subtleData);

        if (publicData.VisibilityRange < subtleData.VisibilityRange)
            publicData.VisibilityRange = subtleData.VisibilityRange;
    }

    protected void TrimData(ref CustomExamineData data)
    {
        // Shitty way to preserve and ignore markup while trimming
        if (data.Content is not null)
        {
            var markupLength = MarkupLength(data.Content);
            if (data.Content.Length > AbsolutelyMaxLength)
                data.Content = data.Content[..AbsolutelyMaxLength];
            if (data.Content.Length - markupLength > PublicMaxLength)
                data.Content = data.Content[..(PublicMaxLength - markupLength)];

            data.Content = data.Content.Trim();
            if (data.Content.Length == 0)
                data.Content = null;
        }
    }

    protected int LengthWithoutMarkup(string text) => FormattedMessage.RemoveMarkupPermissive(text).Length;

    protected int MarkupLength(string text) => text.Length - LengthWithoutMarkup(text);
}
