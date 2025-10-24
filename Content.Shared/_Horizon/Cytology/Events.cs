using Content.Shared._Horizon.Cytology.Components;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared._Horizon.Cytology.Components;

namespace Content.Shared._Horizon.Cytology;

[Serializable, NetSerializable]
public sealed partial class CytologySwabTakeDirtDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class CytologySwabTransferToPetriDishDoAfterEvent : SimpleDoAfterEvent
{
    public readonly List<CellSample> CellSamples;

    public CytologySwabTransferToPetriDishDoAfterEvent(List<CellSample> cellSamples)
    {
        CellSamples = cellSamples;
    }
}

[Serializable, NetSerializable]
public sealed partial class CytologyTransferDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class CytologyGrowingVatIndicatorUpdateAppearance : EntityEventArgs
{
}
