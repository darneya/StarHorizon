using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
namespace Content.Shared.Aavikko;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BaseLayerIdComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<HumanoidSpeciesSpriteLayer>? Layer;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BaseLayerIdToggledComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<HumanoidSpeciesSpriteLayer>? Layer;
}
