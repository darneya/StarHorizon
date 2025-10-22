using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Cytology.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CytologyPetriDishComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<CellSample> CellSamples = new(); //TODO переименовать  CurrentCellSamples or samething like this

    [DataField, AutoNetworkedField]
    public bool IsUsed = false;

    [DataField]
    public int MaxSamples = 5;
}
