using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.AnCo;

[RegisterComponent, NetworkedComponent]
public sealed partial class IgniteOnThrowHitComponent : Component
{
    [DataField]
    public float FireStacks = 1f;
}
