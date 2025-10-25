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

[RegisterComponent, NetworkedComponent]
public sealed partial class CytologyInjectorComponent : Component
{
    [DataField]
    public float TakeDelay = 2f;
}
