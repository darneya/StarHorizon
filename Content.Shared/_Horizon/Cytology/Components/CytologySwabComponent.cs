using Robust.Shared.GameStates;
using Robust.Shared.Serialization;


namespace Content.Shared._Horizon.Cytology.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CytologySwabComponent : Component
{
    [DataField]
    public float SwabDelay = 2f;

    [DataField, AutoNetworkedField]
    public List<CellSample> CellSamples = new();

    [DataField, AutoNetworkedField]
    public bool IsUsed = false;

    [DataField]
    public int MaxSamples = 3;
}
