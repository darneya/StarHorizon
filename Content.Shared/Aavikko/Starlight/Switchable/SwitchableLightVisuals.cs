using Robust.Shared.Serialization;

namespace Content.Shared.Aavikko.Starlight.Switchable;

// Appearance Data key
[Serializable, NetSerializable]
public enum SwitchableLightVisuals : byte
{
    Enabled,
    Color
}
