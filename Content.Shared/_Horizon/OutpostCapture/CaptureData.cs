using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.OutpostCapture;

[Serializable, NetSerializable]
public sealed class OutpostCaptureButtonPressed : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class OutpostCaptureUIStateCall : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ProgressBarUpdate(float? value) : BoundUserInterfaceMessage
{
    public float? Value = value;
}

[Serializable, NetSerializable]
public enum CaptureUIKey : byte
{
    Key = 0,
}
