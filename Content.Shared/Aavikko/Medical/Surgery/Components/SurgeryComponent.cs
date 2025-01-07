using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Aavikko.Medical.Surgery;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSurgerySystem))]
[EntityCategory("Surgeries")]
public sealed partial class SurgeryComponent : Component
{
    [DataField, AutoNetworkedField, Access(typeof(SharedSurgerySystem), Other = AccessPermissions.ReadWriteExecute)]
    public int Priority;

    [DataField, AutoNetworkedField]
    public EntProtoId? Requirement;

    [DataField(required: true), AutoNetworkedField]
    public List<EntProtoId> Steps = new();
}
