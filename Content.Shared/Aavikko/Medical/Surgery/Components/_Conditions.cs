using Content.Shared.Body.Part;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Aavikko.Medical.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class SurgeryAnyAccentConditionComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class SurgeryAnyLimbSlotConditionComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class SurgeryOperatingTableConditionComponent : Component;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryPartConditionComponent : Component
{
    [DataField]
    public List<BodyPartType> Parts = [];
}
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryOrganExistConditionComponent : Component
{
    [DataField]
    public ComponentRegistry? Organ;
}
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryOrganDontExistConditionComponent : Component
{
    [DataField]
    public ComponentRegistry? Organ;
}
