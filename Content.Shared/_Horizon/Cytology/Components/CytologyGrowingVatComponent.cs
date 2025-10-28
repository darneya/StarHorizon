using Robust.Shared.GameStates;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._Horizon.Cytology.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class CytologyGrowingVatComponent : Component
{
    [DataField]
    public ItemSlot PetriDishSlot = new();

    [DataField]
    public ItemSlot BeakerSlot = new();

    [DataField, AutoNetworkedField]
    public bool IsActive = false;

    [DataField, AutoNetworkedField]
    public bool StopWithError = false;

    [DataField]
    public bool WithFoam = false;

    [DataField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField, AutoPausedField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1f);

}
