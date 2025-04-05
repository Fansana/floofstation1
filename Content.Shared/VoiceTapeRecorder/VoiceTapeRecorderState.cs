using Robust.Shared.Serialization;
namespace Content.Shared.VoiceTapeRecorder;

// Ejected is never used as a state for a recorder.
// It's only for specifying the specific visual.
[Serializable, NetSerializable]
public enum RecorderState : byte
{
    Idle,
    Recording,
    Playing,
    Ejected
}


[Serializable, NetSerializable]
public enum RecorderVisuals : byte
{
    State
}
