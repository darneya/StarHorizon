using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Medical.Surgery.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryTargetComponent : Component;
