using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.OutpostCapture;

[Serializable, NetSerializable]
public sealed class OutpostChangeState : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public enum OutpostConsoleState : byte
{
    Capturing,
    Uncaptured,
    Captured,
}
