using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.AnCo;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BlockingOnWieldComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsWielded;
}
