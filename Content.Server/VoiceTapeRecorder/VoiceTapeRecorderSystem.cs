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

    private TimeSpan? _nextRecorderCheck;

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

    public void ScheduleNextRecorder(VoiceTapeRecorderComponent component)
    {
        if (
            TryGetCassetteComponent(component, out var cassette) &&
            (
                component.State == RecorderState.Playing ||
                component.State == RecorderState.Recording
            )
        )
        {
            var offset = component.PlayRecordingStarted - component.TimeShift;
            TimeSpan when;
            if (component.State == RecorderState.Playing)
            {
                if (component.NextMessageIndex < cassette.RecordedMessages.Count)
                    when = offset + cassette.RecordedMessages[component.NextMessageIndex].When;
                else
                    when = offset + cassette.RecordedSoFar;

                if (component.SkipSilence)
                {
                    var currentTime = _timing.CurTime;
                    var diff = when - currentTime;
                    if (diff > _maxSilence)
                    {
                        when = currentTime + _maxSilence;
                        component.TimeShift += diff - _maxSilence;
                    }
                }
            }
            else // Recording
            {
                when = component.PlayRecordingStarted + cassette.Capacity - cassette.RecordedSoFar;
            }
            component.WhenToDoSomething = when;
            if (_nextRecorderCheck is null || _nextRecorderCheck > when)
                _nextRecorderCheck = when;
        }
    }

    private void Say(
        EntityUid uid,
        VoiceTapeRecorderComponent component,
        string message
    )
    {
        var chatType = component.HighVolume ? InGameICChatType.Speak : InGameICChatType.Whisper;
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
        VoiceTapeRecorderComponent component,
        RecordedMessage recordedMessage
    )
    {
        var chatType = component.HighVolume ? InGameICChatType.Speak : InGameICChatType.Whisper;
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
        if (_nextRecorderCheck is null) return;
        var currentTime = _timing.CurTime;
        if (currentTime < _nextRecorderCheck) return;
        _nextRecorderCheck = null;

        var query = EntityQueryEnumerator<VoiceTapeRecorderComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.State == RecorderState.Idle) continue;
            if (currentTime >= component.WhenToDoSomething)
            {
                switch (component.State)
                {
                    case RecorderState.Playing:
                        HandlePlayingRecorder(uid, component);
                        break;
                    case RecorderState.Recording:
                        HandleRecordingRecorder(uid, component);
                        break;
                    default: break;
                }
            }
            var timeShift = component.TimeShift;
            ScheduleNextRecorder(component);
            if (timeShift != component.TimeShift)
                _audioSystem.PlayPvs(
                    component.SeekSound,
                    uid
                );
        }
    }

    private void HandleRecordingRecorder(
        EntityUid uid,
        VoiceTapeRecorderComponent component
    ) => ChangeState(uid, component, RecorderState.Idle);

    private void HandlePlayingRecorder(
        EntityUid uid,
        VoiceTapeRecorderComponent component
    )
    {
        if (
            TryGetCassetteComponent(component, out var cassette) &&
            component.NextMessageIndex < cassette.RecordedMessages.Count)
        {
            Impersonate(uid, component,
                cassette.RecordedMessages[component.NextMessageIndex++]
            );
        }
        else
        {
            //Say(uid, component, Loc.GetString("voice-tape-recorder-end-of-tape"));
            ChangeState(uid, component, RecorderState.Idle);
        }
    }

    private void OnInit(
        EntityUid uid,
        VoiceTapeRecorderComponent component,
        ComponentInit args
    )
    {
        EnsureComp<SpeechComponent>(uid);
        component.Cassette = _containerSystem.EnsureContainer<ContainerSlot>(uid, $"recorder-cassette");
        ChangeState(uid, component, component.State);
    }

    private void OnListen(
        EntityUid uid,
        VoiceTapeRecorderComponent component,
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

        if (TryGetCassetteComponent(component, out var cassette))
            cassette.RecordedMessages.Add(
                new RecordedMessage(
                    cassette.RecordedSoFar + (_timing.CurTime - component.PlayRecordingStarted),
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

    private void UpdateAppearance(
        EntityUid uid,
        VoiceTapeRecorderComponent component,
        AppearanceComponent? appearance = null
    )
    {
        if (!Resolve(uid, ref appearance, false))
            return;

        if (TryGetCassette(component, out _))
            _appearance.SetData(uid, RecorderVisuals.State, component.State, appearance);
        else
            _appearance.SetData(uid, RecorderVisuals.State, RecorderState.Ejected, appearance);
    }

    private void ChangeState(
        EntityUid uid,
        VoiceTapeRecorderComponent component,
        RecorderState to
    )
    {
        var from = component.State;
        if (!TryGetCassetteComponent(component, out _))
            to = RecorderState.Idle;

        if (from != to) // Prevents freshly-spawned tapes clicking (Idle to Idle)
        {
            component.State = to;
            OnStateChange(uid, component, from, to);
        }
        UpdateAppearance(uid, component);
    }

    private void OnStateChange(
        EntityUid uid,
        VoiceTapeRecorderComponent component,
        RecorderState from,
        RecorderState to
    )
    {
        if (
            from == RecorderState.Recording &&
            TryGetCassetteComponent(component, out var cassette)
        )
            cassette.RecordedSoFar = cassette.RecordedSoFar + _timing.CurTime - component.PlayRecordingStarted;

        if (to == RecorderState.Recording || to == RecorderState.Playing)
        {
            component.TimeShift = TimeSpan.Zero;
            component.PlayRecordingStarted = _timing.CurTime;
            if (to == RecorderState.Playing)
            {
                component.NextMessageIndex = 0;
                ScheduleNextRecorder(component);
            }
            else // Recording
            {
                EnsureComp<ActiveListenerComponent>(uid).Range = component.ListenRange;
                ScheduleNextRecorder(component);
            }
        }
        if (to != RecorderState.Recording)
            RemCompDeferred<ActiveListenerComponent>(uid);

        _audioSystem.PlayPvs(
            to == RecorderState.Idle ? component.StopSound : component.StartSound,
            uid
        );
        _ambientSoundSystem.SetAmbience(uid, to != RecorderState.Idle);
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
        if (TryGetCassetteComponent(component, out var cassette))
        {
            cassette.RecordedSoFar = TimeSpan.Zero;
            cassette.RecordedMessages = [];
        }
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

    private static bool TryGetCassette(
        VoiceTapeRecorderComponent component,
        [NotNullWhen(true)] out EntityUid? cassette
    )
    {
        if (component.Cassette.ContainedEntity is { Valid: true } contained)
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
        VoiceTapeRecorderComponent component,
        [NotNullWhen(true)] out VoiceTapeRecorderCassetteComponent? cassette
    )
    {
        cassette = null;
        return
            TryGetCassette(component, out var ent) &&
            TryComp(ent, out cassette);
    }

    private void EjectCassette(
        EntityUid uid,
        VoiceTapeRecorderComponent component,
        EntityUid? who
    )
    {
        if (TryGetCassette(component, out var cassette))
        {
            ChangeState(uid, component, RecorderState.Idle);
            if (_containerSystem.Remove(cassette.Value, component.Cassette))
            {
                _audioSystem.PlayPvs(
                    component.EjectSound,
                    uid
                );
                UpdateAppearance(uid, component);
            }
        }
    }

    private void InsertCassette(
        EntityUid uid,
        VoiceTapeRecorderComponent component,
        EntityUid insert,
        EntityUid? who
    )
    {
        if (!HasComp<VoiceTapeRecorderCassetteComponent>(insert))
            return;

        if (component.Cassette.ContainedEntity != null)
            EjectCassette(uid, component, who);

        if (_containerSystem.Insert(insert, component.Cassette))
        {
            _audioSystem.PlayPvs(
                component.InsertSound,
                uid
            );
            UpdateAppearance(uid, component);
        }
    }
    private void OnInteractUsing(
        EntityUid uid,
        VoiceTapeRecorderComponent component,
        InteractUsingEvent args
    )
    => InsertCassette(uid, component, args.Used, args.User);

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
            Text = component.HighVolume ?
                Loc.GetString("voice-tape-recorder-volume-low") :
                Loc.GetString("voice-tape-recorder-volume-high"),
            Act = () => component.HighVolume = !component.HighVolume,
            Priority = 5
        });

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("voice-tape-recorder-eject"),
            Act = () => EjectCassette(uid, component, args.User),
            Priority = 3
        });

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = component.SkipSilence ?
                Loc.GetString("voice-tape-recorder-skip-silence-off") :
                Loc.GetString("voice-tape-recorder-skip-silence-on"),
            Act = () => component.SkipSilence = !component.SkipSilence,
            Priority = 2
        });

        if (component.State == RecorderState.Idle)
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
    Idle,
    Playing,
    Recording,
    Ejected
}
