using Content.Shared.Language;

namespace Content.Server.VoiceTapeRecorder;

[RegisterComponent]
[Access(typeof(VoiceTapeRecorderSystem))]
public sealed partial class VoiceTapeRecorderComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public RecorderState State = RecorderState.Idle;
    [ViewVariables(VVAccess.ReadOnly)]
    public List<RecordedMessage> RecordedMessages = [];
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan RecordedSoFar = TimeSpan.Zero;
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan PlayRecordingStarted = TimeSpan.Zero;
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan WhenToSayNextMessage = TimeSpan.Zero;
    [ViewVariables(VVAccess.ReadOnly)]
    public int NextMessageIndex = 0;
    [ViewVariables(VVAccess.ReadWrite)]
    public int ListenRange = 4;
    [ViewVariables(VVAccess.ReadWrite)]
    public bool NormalVolume = false;
}

public enum RecorderState
{
    Idle,
    Recording,
    Playing
}