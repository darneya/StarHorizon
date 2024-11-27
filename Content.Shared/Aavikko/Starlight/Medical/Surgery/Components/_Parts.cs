using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Aavikko.Starlight.Medical.Surgery.Steps.Parts;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class IncisionOpenComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class SkinRetractedComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class BleedersClampedComponent : Component;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryStepOrganExtractComponent : Component
{
    [DataField]
    public ComponentRegistry? Organ;
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryStepOrganInsertComponent : Component
{
    [DataField(required: true)]
    public string Slot;
}
