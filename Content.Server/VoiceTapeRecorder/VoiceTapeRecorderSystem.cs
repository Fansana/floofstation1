using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Server.Language;
using Content.Shared.Language;
using Content.Shared.Language.Components;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;

namespace Content.Server.VoiceTapeRecorder;

public sealed class VoiceTapeRecorderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private TimeSpan? nextPlayCheck;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VoiceTapeRecorderComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<VoiceTapeRecorderComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<VoiceTapeRecorderComponent, ListenAttemptEvent>(OnAttemptListen);

        SubscribeLocalEvent<VoiceTapeRecorderComponent, GetVerbsEvent<ActivationVerb>>(OnActivateVerb);
        SubscribeLocalEvent<VoiceTapeRecorderComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<VoiceTapeRecorderComponent, GetVerbsEvent<AlternativeVerb>>(OnAltActivateVerb);
    }

    public void ScheduleNextRecorder(VoiceTapeRecorderComponent component)
    {
        var offset = component.PlayRecordingStarted;
        TimeSpan when;
        if (component.NextMessageIndex < component.RecordedMessages.Count)
            when = offset + component.RecordedMessages[component.NextMessageIndex].When;
        else
            when = offset + component.RecordedSoFar;
        component.WhenToSayNextMessage = when;
        if (nextPlayCheck is null || nextPlayCheck > when)
            nextPlayCheck = when;
    }

    public override void Update(float frameTime)
    {
        if (nextPlayCheck is null) return;
        var currentTime = _timing.CurTime;
        if (currentTime < nextPlayCheck) return;
        nextPlayCheck = null;

        var query = EntityQueryEnumerator<VoiceTapeRecorderComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.State != RecorderState.Playing) continue;
            if (currentTime >= component.WhenToSayNextMessage)
            {
                var chatType = component.NormalVolume ? InGameICChatType.Speak : InGameICChatType.Whisper;
                if (component.NextMessageIndex < component.RecordedMessages.Count)
                {
                    var language = EnsureComp<LanguageSpeakerComponent>(uid);
                    var voiceOverride = EnsureComp<VoiceOverrideComponent>(uid);
                    var m = component.RecordedMessages[component.NextMessageIndex];
                    voiceOverride.NameOverride = m.Who;
                    voiceOverride.SpeechVerbOverride = m.Speech;
                    voiceOverride.Enabled = true;
                    language.CurrentLanguage = m.Language.ID;
                    _chat.TrySendInGameICMessage(
                        uid,
                        m.Message,
                        chatType,
                        ChatTransmitRange.GhostRangeLimit,
                        checkRadioPrefix: false
                    );
                    component.NextMessageIndex++;
                }
                else
                {
                    var language = EnsureComp<LanguageSpeakerComponent>(uid);
                    language.CurrentLanguage = Shared.Language.Systems.SharedLanguageSystem.Universal.ID;
                    if (TryComp<VoiceOverrideComponent>(uid, out var voiceOverride))
                    {
                        voiceOverride.Enabled = false;
                        RemCompDeferred<VoiceOverrideComponent>(uid);
                    }
                    _chat.TrySendInGameICMessage(
                        uid,
                        Loc.GetString("voice-tape-recorder-end-of-tape"),
                        chatType,
                        ChatTransmitRange.GhostRangeLimit,
                        checkRadioPrefix: false
                    );
                    ChangeState(uid, component, RecorderState.Idle);
                    RemCompDeferred<LanguageSpeakerComponent>(uid);
                }
            }
            ScheduleNextRecorder(component);
        }
    }

    private void OnInit(
        EntityUid uid,
        VoiceTapeRecorderComponent component,
        ComponentInit args
    )
    {
        EnsureComp<SpeechComponent>(uid);
        OnStateChange(uid, component, RecorderState.Idle, component.State);
    }

    private void OnListen(EntityUid uid, VoiceTapeRecorderComponent component, ListenEvent args)
    {
        // Name and Speech finding ripped from Content.Server.Chat.Systems.SendEntitySpeak
        var speech = _chat.GetSpeechVerb(args.Source, args.Message);
        var nameEv = new TransformSpeakerNameEvent(args.Source, Name(args.Source));
        RaiseLocalEvent(args.Source, nameEv);
        var name = nameEv.VoiceName ?? Name(args.Source);
        // Check for a speech verb override
        if (nameEv.SpeechVerb != null && _prototypeManager.TryIndex(nameEv.SpeechVerb, out var proto))
            speech = proto;

        component.RecordedMessages.Add(
            new RecordedMessage(
                component.RecordedSoFar + (_timing.CurTime - component.PlayRecordingStarted),
                name,
                speech,
                _language.GetLanguage(args.Source),
                args.Message
            )
        );
    }

    private void OnAttemptListen(
        EntityUid uid,
        VoiceTapeRecorderComponent component,
        ListenAttemptEvent args
    )
    {
        if (component.State != RecorderState.Recording) args.Cancel();
    }

    private void ChangeState(
        EntityUid uid,
        VoiceTapeRecorderComponent component,
        RecorderState to
    )
    {
        if(component.State == to) return;
        var from = component.State;
        component.State = to;
        OnStateChange(uid, component, from, to);
    }

    private void OnStateChange(
        EntityUid uid,
        VoiceTapeRecorderComponent component,
        RecorderState from,
        RecorderState to
    )
    {
        if (from == RecorderState.Recording)
            component.RecordedSoFar = component.RecordedSoFar + _timing.CurTime - component.PlayRecordingStarted;
        if (to == RecorderState.Recording || to == RecorderState.Playing)
        {
            component.PlayRecordingStarted = _timing.CurTime;
            if (to == RecorderState.Playing)
            {
                component.NextMessageIndex = 0;
                ScheduleNextRecorder(component);
            }
            else if (to == RecorderState.Recording)
                EnsureComp<ActiveListenerComponent>(uid).Range = component.ListenRange;
        }
        else
        {
            RemCompDeferred<ActiveListenerComponent>(uid);
        }
    }

    private void OnUse(
        EntityUid uid,
        VoiceTapeRecorderComponent component
    )
    => ChangeState(uid, component,
        component.State != RecorderState.Playing ?
            RecorderState.Playing : RecorderState.Idle
    );

    private void OnAltUse(
        EntityUid uid,
        VoiceTapeRecorderComponent component
    )
    => ChangeState(uid, component,
        component.State != RecorderState.Recording ?
            RecorderState.Recording : RecorderState.Idle
    );

    private void EraseTape(
        EntityUid uid,
        VoiceTapeRecorderComponent component
    )
    {
        ChangeState(uid, component, RecorderState.Idle);
        component.RecordedSoFar = TimeSpan.Zero;
        component.RecordedMessages = [];
    }

    private void OnActivate(
        EntityUid uid,
        VoiceTapeRecorderComponent component,
        ActivateInWorldEvent args
    )
    => OnUse(uid, component);

    private void OnActivateVerb(
        EntityUid uid,
        VoiceTapeRecorderComponent component,
        GetVerbsEvent<ActivationVerb> args
    )
    => args.Verbs.Add(new ActivationVerb()
    {
        Text = Loc.GetString("voice-tape-recorder-play"),
        Act = () => OnUse(uid, component)
    });

    private void OnAltActivateVerb(
        EntityUid uid,
        VoiceTapeRecorderComponent component,
        GetVerbsEvent<AlternativeVerb> args
    )
    {
        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("voice-tape-recorder-record"),
            Act = () => OnAltUse(uid, component),
            Priority = 10
        });
        args.Verbs.Add(new AlternativeVerb()
        {
            Text = component.NormalVolume ?
                Loc.GetString("voice-tape-recorder-volume-low") :
                Loc.GetString("voice-tape-recorder-volume-high"),
            Act = () => component.NormalVolume = !component.NormalVolume,
            Priority = 5
        });
        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("voice-tape-recorder-erase"),
            Act = () => EraseTape(uid, component),
            Priority = 1
        });
    }
}

public record struct RecordedMessage(
    TimeSpan When,
    string Who,
    SpeechVerbPrototype Speech,
    LanguagePrototype Language,
    string Message
);

public enum RecorderLayers : byte
{
    Icon,
    Playing,
    Recording,
}
