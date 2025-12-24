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
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<NpcFactionPrototype>? FactionCaptured { get; set; }

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
