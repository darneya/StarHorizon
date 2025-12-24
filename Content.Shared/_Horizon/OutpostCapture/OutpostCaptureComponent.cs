using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.OutpostCapture;

/// <summary>
/// Используется для регистрации grid, которые можно захватить
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class OutpostCaptureComponent : Component
{
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<Entity<OutpostConsoleComponent>> CapturingConsoles;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public int CapturedConsoles { get; set; }

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? SpawningPointUid = null;

    [DataField]
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int NeedCapturedConsoles = 1;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<OutpostSpawnList>? SpawnList;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? ActualSpawnCooldown = null;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SpawnCooldown = TimeSpan.FromMinutes(1);

    [DataField]
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<Entity<OutpostConsoleComponent>> LinkedConsoles;
}
