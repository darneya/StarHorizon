using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.OutpostCapture;

/// <summary>
/// Используется для регистрации grid, которые можно захватить
/// </summary>
[RegisterComponent]
public sealed partial class OutpostCaptureComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<OutpostSpawnList>? SpawnList;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> LinkedConsoles;
}
