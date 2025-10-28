using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Cytology.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CytologyDirtComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<CellSample> PossibleCellSamples = new();

    [DataField, AutoNetworkedField] // TODO убрать датафилд
    public List<CellSample> CurrentCellSamples = new();

    [DataField]
    public float SampleChance = 0.7f;

    [DataField]
    public int MaxSamples = 3;
}
