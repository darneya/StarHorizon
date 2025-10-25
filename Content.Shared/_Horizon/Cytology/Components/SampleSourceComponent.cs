using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Cytology.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SampleSourceComponent : Component
{
    [DataField]
    public List<CellSample> AvailableCellSamples;
}
