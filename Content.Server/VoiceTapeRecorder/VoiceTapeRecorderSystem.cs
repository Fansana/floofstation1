using System.Diagnostics.CodeAnalysis;
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
using Content.Shared.Audio;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Content.Shared.VoiceTapeRecorder;

namespace Content.Server.VoiceTapeRecorder;

public sealed class VoiceTapeRecorderSystem : EntitySystem
{
    private readonly TimeSpan _maxSilence = new TimeSpan(0, 0, 5);
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;

    private TimeSpan? _whenNextEvent;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VoiceTapeRecorderComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<VoiceTapeRecorderComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<VoiceTapeRecorderComponent, ListenAttemptEvent>(OnAttemptListen);

        SubscribeLocalEvent<VoiceTapeRecorderComponent, GetVerbsEvent<ActivationVerb>>(OnActivateVerb);
        SubscribeLocalEvent<VoiceTapeRecorderComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<VoiceTapeRecorderComponent, GetVerbsEvent<AlternativeVerb>>(OnAltActivateVerb);

        SubscribeLocalEvent<VoiceTapeRecorderComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private record struct ScheduleRecorderResult(
        bool TimeShifted
    );

    private ScheduleRecorderResult ScheduleNextRecorder(VoiceTapeRecorderComponent recorder)
    {
        var timeShifted = false;
        if (
            TryGetCassetteComponent(recorder, out var cassette) &&
            recorder.State != RecorderState.Idle
        )
        {
            var offset = recorder.PlayRecordingStarted - recorder.TimeShift;
            TimeSpan when;
            if (recorder.State == RecorderState.Playing)
            {
                if (recorder.NextMessageIndex < cassette.RecordedMessages.Count)
                    when = offset + cassette.RecordedMessages[recorder.NextMessageIndex].When;
                else
                    when = offset + cassette.RecordedSoFar;

                if (recorder.SkipSilence)
                {
                    var currentTime = _timing.CurTime;
                    var diff = when - currentTime;
                    if (diff > _maxSilence)
                    {
                        when = currentTime + _maxSilence;
                        recorder.TimeShift += diff - _maxSilence;
                        timeShifted = true;
                    }
                }
            }
            else // Recording
            {
                when = recorder.PlayRecordingStarted + cassette.Capacity - cassette.RecordedSoFar;
            }
            recorder.WhenNextEvent = when;
            if (_whenNextEvent is null || _whenNextEvent > when)
                _whenNextEvent = when;
        }
        return new ScheduleRecorderResult(timeShifted);
    }

    private void Say(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder,
        string message
    )
    {
        var chatType = recorder.HighVolume ? InGameICChatType.Speak : InGameICChatType.Whisper;
        var language = EnsureComp<LanguageSpeakerComponent>(uid);
        language.CurrentLanguage = Shared.Language.Systems.SharedLanguageSystem.Universal.ID;
        if (TryComp<VoiceOverrideComponent>(uid, out var voiceOverride))
            voiceOverride.Enabled = false;
        _chat.TrySendInGameICMessage(
            uid,
            message,
            chatType,
            ChatTransmitRange.GhostRangeLimit,
            checkRadioPrefix: false
        );
    }

    private void Impersonate(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder,
        RecordedMessage recordedMessage
    )
    {
        var chatType = recorder.HighVolume ? InGameICChatType.Speak : InGameICChatType.Whisper;
        var language = EnsureComp<LanguageSpeakerComponent>(uid);
        var voiceOverride = EnsureComp<VoiceOverrideComponent>(uid);
        voiceOverride.NameOverride = recordedMessage.Who;
        voiceOverride.SpeechVerbOverride = recordedMessage.Speech;
        voiceOverride.Enabled = true;
        language.CurrentLanguage = recordedMessage.Language.ID;
        _chat.TrySendInGameICMessage(
            uid,
            recordedMessage.Message,
            chatType,
            ChatTransmitRange.GhostRangeLimit,
            checkRadioPrefix: false
        );
    }

    public override void Update(float frameTime)
    {
        if (_whenNextEvent is null) return;
        var currentTime = _timing.CurTime;
        if (currentTime < _whenNextEvent) return;
        _whenNextEvent = null;

        var query = EntityQueryEnumerator<VoiceTapeRecorderComponent>();
        while (query.MoveNext(out var uid, out var recorder))
        {
            if (recorder.State == RecorderState.Idle) continue;
            if (currentTime >= recorder.WhenNextEvent)
            {
                switch (recorder.State)
                {
                    case RecorderState.Playing:
                        OnPlayingEvent(uid, recorder);
                        break;
                    case RecorderState.Recording:
                        OnRecordingEvent(uid, recorder);
                        break;
                    default: break;
                }
            }
            if (ScheduleNextRecorder(recorder).TimeShifted)
                _audioSystem.PlayPvs(
                    recorder.SeekSound,
                    uid
                );
        }
    }

    private void OnRecordingEvent(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder
    ) => ChangeState(uid, recorder, RecorderState.Idle);

    private void OnPlayingEvent(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder
    )
    {
        if (
            TryGetCassetteComponent(recorder, out var cassette) &&
            recorder.NextMessageIndex < cassette.RecordedMessages.Count)
        {
            Impersonate(uid, recorder,
                cassette.RecordedMessages[recorder.NextMessageIndex++]
            );
        }
        else
        {
            //Say(uid, component, Loc.GetString("voice-tape-recorder-end-of-tape"));
            ChangeState(uid, recorder, RecorderState.Idle);
        }
    }

    private void OnInit(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder,
        ComponentInit args
    )
    {
        EnsureComp<SpeechComponent>(uid);
        recorder.Cassette = _containerSystem.EnsureContainer<ContainerSlot>(uid, $"recorder-cassette");
        ChangeState(uid, recorder, recorder.State);
    }

    private void OnListen(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder,
        ListenEvent args
    )
    {
        // Name and Speech finding ripped from Content.Server.Chat.Systems.SendEntitySpeak
        var speech = _chat.GetSpeechVerb(args.Source, args.Message);
        var nameEv = new TransformSpeakerNameEvent(args.Source, Name(args.Source));
        RaiseLocalEvent(args.Source, nameEv);
        var name = nameEv.VoiceName ?? Name(args.Source);
        // Check for a speech verb override
        if (nameEv.SpeechVerb != null && _prototypeManager.TryIndex(nameEv.SpeechVerb, out var proto))
            speech = proto;

        if (TryGetCassetteComponent(recorder, out var cassette))
            cassette.RecordedMessages.Add(
                new RecordedMessage(
                    cassette.RecordedSoFar + (_timing.CurTime - recorder.PlayRecordingStarted),
                    name,
                    speech,
                    _language.GetLanguage(args.Source),
                    args.Message
                )
            );
    }

    private void OnAttemptListen(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder,
        ListenAttemptEvent args
    )
    {
        if (recorder.State != RecorderState.Recording) args.Cancel();
    }

    private void UpdateAppearance(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder,
        AppearanceComponent? appearance = null
    )
    {
        if (!Resolve(uid, ref appearance, false))
            return;

        if (TryGetCassette(recorder, out _))
            _appearance.SetData(uid, RecorderVisuals.State, recorder.State, appearance);
        else
            _appearance.SetData(uid, RecorderVisuals.State, RecorderState.Ejected, appearance);
    }

    private void ChangeState(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder,
        RecorderState to
    )
    {
        var from = recorder.State;
        if (!TryGetCassetteComponent(recorder, out _))
            to = RecorderState.Idle;

        if (from != to) // Prevents freshly-spawned tapes clicking (Idle to Idle)
        {
            recorder.State = to;
            OnStateChange(uid, recorder, from, to);
        }
        UpdateAppearance(uid, recorder);
    }

    private void OnStateChange(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder,
        RecorderState from,
        RecorderState to
    )
    {
        if (
            from == RecorderState.Recording &&
            TryGetCassetteComponent(recorder, out var cassette)
        )
            cassette.RecordedSoFar = cassette.RecordedSoFar + _timing.CurTime - recorder.PlayRecordingStarted;

        if (to != RecorderState.Idle)
        {
            recorder.TimeShift = TimeSpan.Zero;
            recorder.PlayRecordingStarted = _timing.CurTime;
            if (to == RecorderState.Playing)
            {
                recorder.NextMessageIndex = 0;
                ScheduleNextRecorder(recorder);
            }
            else // Recording
            {
                EnsureComp<ActiveListenerComponent>(uid).Range = recorder.ListenRange;
                ScheduleNextRecorder(recorder);
            }
        }
        if (to != RecorderState.Recording)
            RemCompDeferred<ActiveListenerComponent>(uid);

        _audioSystem.PlayPvs(
            to == RecorderState.Idle ? recorder.StopSound : recorder.StartSound,
            uid
        );
        _ambientSoundSystem.SetAmbience(uid, to != RecorderState.Idle);
    }

    private void OnUse(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder
    )
    => ChangeState(uid, recorder,
        recorder.State != RecorderState.Playing ?
            RecorderState.Playing : RecorderState.Idle
    );

    private void OnAltUse(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder
    )
    => ChangeState(uid, recorder,
        recorder.State != RecorderState.Recording ?
            RecorderState.Recording : RecorderState.Idle
    );

    private void EraseTape(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder
    )
    {
        ChangeState(uid, recorder, RecorderState.Idle);
        if (TryGetCassetteComponent(recorder, out var cassette))
        {
            cassette.RecordedSoFar = TimeSpan.Zero;
            cassette.RecordedMessages = [];
        }
    }

    private void OnActivate(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder,
        ActivateInWorldEvent args
    )
    => OnUse(uid, recorder);

    private void OnActivateVerb(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder,
        GetVerbsEvent<ActivationVerb> args
    )
    => args.Verbs.Add(new ActivationVerb()
    {
        Text = Loc.GetString("voice-tape-recorder-play"),
        Act = () => OnUse(uid, recorder)
    });

    private static bool TryGetCassette(
        VoiceTapeRecorderComponent recorder,
        [NotNullWhen(true)] out EntityUid? cassette
    )
    {
        if (recorder.Cassette.ContainedEntity is { Valid: true } contained)
        {
            cassette = contained;
            return true;
        }
        else
        {
            cassette = null;
            return false;
        }
    }

    private bool TryGetCassetteComponent(
        VoiceTapeRecorderComponent recorder,
        [NotNullWhen(true)] out VoiceTapeRecorderCassetteComponent? cassette
    )
    {
        cassette = null;
        return
            TryGetCassette(recorder, out var ent) &&
            TryComp(ent, out cassette);
    }

    private void EjectCassette(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder,
        EntityUid? who
    )
    {
        if (TryGetCassette(recorder, out var cassette))
        {
            ChangeState(uid, recorder, RecorderState.Idle);
            if (_containerSystem.Remove(cassette.Value, recorder.Cassette))
            {
                _audioSystem.PlayPvs(
                    recorder.EjectSound,
                    uid
                );
                UpdateAppearance(uid, recorder);
            }
        }
    }

    private void InsertCassette(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder,
        EntityUid insert,
        EntityUid? who
    )
    {
        if (!HasComp<VoiceTapeRecorderCassetteComponent>(insert))
            return;

        if (recorder.Cassette.ContainedEntity != null)
            EjectCassette(uid, recorder, who);

        if (_containerSystem.Insert(insert, recorder.Cassette))
        {
            _audioSystem.PlayPvs(
                recorder.InsertSound,
                uid
            );
            UpdateAppearance(uid, recorder);
        }
    }
    private void OnInteractUsing(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder,
        InteractUsingEvent args
    )
    => InsertCassette(uid, recorder, args.Used, args.User);

    private void OnAltActivateVerb(
        EntityUid uid,
        VoiceTapeRecorderComponent recorder,
        GetVerbsEvent<AlternativeVerb> args
    )
    {
        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("voice-tape-recorder-record"),
            Act = () => OnAltUse(uid, recorder),
            Priority = 10
        });
        args.Verbs.Add(new AlternativeVerb()
        {
            Text = recorder.HighVolume ?
                Loc.GetString("voice-tape-recorder-volume-low") :
                Loc.GetString("voice-tape-recorder-volume-high"),
            Act = () => recorder.HighVolume = !recorder.HighVolume,
            Priority = 5
        });

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("voice-tape-recorder-eject"),
            Act = () => EjectCassette(uid, recorder, args.User),
            Priority = 3
        });

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = recorder.SkipSilence ?
                Loc.GetString("voice-tape-recorder-skip-silence-off") :
                Loc.GetString("voice-tape-recorder-skip-silence-on"),
            Act = () => recorder.SkipSilence = !recorder.SkipSilence,
            Priority = 2
        });

        if (recorder.State == RecorderState.Idle)
            args.Verbs.Add(new AlternativeVerb()
            {
                Text = Loc.GetString("voice-tape-recorder-erase"),
                Act = () => EraseTape(uid, recorder),
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
    Idle,
    Playing,
    Recording,
    Ejected
}
