using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Cytology.Components;


[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class CellSample // TODO думать что-то с дубликатом
{
    [DataField]
    public string ProtoID;

    [DataField]
    public float GrowProgress;

    public CellSample(string protoID, float growProgress = 0f)
    {
        ProtoID = protoID;
        GrowProgress = growProgress;
    }
}
