using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Cytology.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CytologyPetriDishComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsUsed = false;
}
