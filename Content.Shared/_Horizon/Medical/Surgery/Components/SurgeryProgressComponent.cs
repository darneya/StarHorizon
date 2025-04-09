using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Medical.Surgery.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryProgressComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId> CompletedSteps = [];

    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId> CompletedSurgeries = [];

    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId> StartedSurgeries = [];
}
