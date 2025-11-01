using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Cytology.Components;


/// <summary>
///     Stores the cell shot along with the growth parameter, which changes during growing
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class CellSample
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
