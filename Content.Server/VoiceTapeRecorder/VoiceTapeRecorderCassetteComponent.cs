using Content.Server.VoiceTapeRecorder;
namespace Content.Server.VoiceTapeRecorder;

[RegisterComponent]
[Access(typeof(VoiceTapeRecorderSystem))]
public sealed partial class VoiceTapeRecorderCassetteComponent : Component
{
    public List<RecordedMessage> RecordedMessages = [];
    public TimeSpan RecordedSoFar = TimeSpan.Zero;
    [DataField]
    public TimeSpan Capacity = new TimeSpan(0, 10, 0);

    public void Commit(TimeSpan recordingStarted, TimeSpan currentTime)
    => RecordedSoFar = RecordedSoFar + currentTime - recordingStarted;
}
