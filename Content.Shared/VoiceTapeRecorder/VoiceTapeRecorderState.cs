using Robust.Shared.Serialization;
namespace Content.Shared.VoiceTapeRecorder;

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
