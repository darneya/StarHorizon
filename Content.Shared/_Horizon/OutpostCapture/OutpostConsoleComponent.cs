using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.OutpostCapture;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class OutpostConsoleComponent : Component
{
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public OutpostConsoleState State { get; set; } = OutpostConsoleState.Uncaptured;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string? FactionCaptured { get; set; }

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? CapturingTime { get; set; }

    [DataField]
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CaptureTime = TimeSpan.FromSeconds(30);

    [DataField]
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public Color? CapturedColor { get; set; }

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? LinkedOutpost { get; set; }
}
