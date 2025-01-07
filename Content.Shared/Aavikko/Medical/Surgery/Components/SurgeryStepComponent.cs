using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Aavikko.Medical.Surgery.Steps;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSurgerySystem))]
[EntityCategory("SurgerySteps")]
public sealed partial class SurgeryStepComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Duration = 2;

    [DataField]
    public ComponentRegistry? Tools;

    [DataField]
    public ComponentRegistry? Add;

    [DataField]
    public ComponentRegistry? BodyAdd;

    [DataField]
    public ComponentRegistry? Remove;

    [DataField]
    public ComponentRegistry? BodyRemove;
}
