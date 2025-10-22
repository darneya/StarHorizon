using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Cytology;

[Serializable, NetSerializable]
public enum CytologyGrowingVatVisuals : byte
{
    Working,
    Powered,
}

[Serializable, NetSerializable]
public enum CytologyGrowingVatVisualLayers : byte
{
    Base,
    Indicator,
    Liquid
}
