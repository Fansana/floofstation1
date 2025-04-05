using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Content.Shared.VoiceTapeRecorder;

namespace Content.Server.VoiceTapeRecorder;

[RegisterComponent]
[Access(typeof(VoiceTapeRecorderSystem))]
public sealed partial class VoiceTapeRecorderComponent : Component
{
    public ContainerSlot Cassette = default!;
    public RecorderState State = RecorderState.Idle;
    public TimeSpan PlayRecordingStarted = TimeSpan.Zero;
    public TimeSpan WhenNextEvent = TimeSpan.Zero;
    public bool SkipSilence = false;
    public TimeSpan TimeShift = TimeSpan.Zero;
    public int NextMessageIndex = 0;
    public int ListenRange = 4;
    public bool HighVolume = false;
    [DataField]
    public SoundSpecifier? StartSound;
    [DataField]
    public SoundSpecifier? StopSound;
    [DataField]
    public SoundSpecifier? InsertSound;
    [DataField]
    public SoundSpecifier? EjectSound;
    [DataField]
    public SoundSpecifier? SeekSound;
}
