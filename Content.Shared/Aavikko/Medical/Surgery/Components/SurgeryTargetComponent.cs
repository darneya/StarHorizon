using Robust.Shared.GameStates;

namespace Content.Shared.Aavikko.Medical.Surgery;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryTargetComponent : Component;
