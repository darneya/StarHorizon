using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.Containers.ItemSlots;
using System;

namespace Content.Shared._Horizon.Cytology.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class CytologyGrowingVatComponent : Component
{
    [DataField]
    public ItemSlot PetriDishSlot = new();

    [DataField]
    public ItemSlot BeakerSlot = new();

    [DataField]
    public bool IsActive = false;

    [DataField]
    public bool StopWithError = false;

    [DataField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField, AutoPausedField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1f);

}
