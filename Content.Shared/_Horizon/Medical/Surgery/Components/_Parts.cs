using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Medical.Surgery.Components;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class IncisionOpenComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SkinRetractedComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class BleedersClampedComponent : Component;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryStepOrganExtractComponent : Component
{
    [DataField]
    public ComponentRegistry? Organ;

    [DataField]
    public string? Slot;
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryStepOrganInsertComponent : Component
{
    [DataField(required: true)]
    public string Slot;
}
