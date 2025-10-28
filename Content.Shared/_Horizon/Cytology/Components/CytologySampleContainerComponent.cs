using Robust.Shared.GameStates;
using Robust.Shared.Serialization;


namespace Content.Shared._Horizon.Cytology.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class CytologySampleContainerComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<CellSample> CellSamples = new();

    [DataField]
    public int MaxSamples = 5;
}
