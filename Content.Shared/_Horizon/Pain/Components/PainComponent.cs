using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Pain.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PainComponent : Component
{
    [DataField]
    public PainStages? PainStage;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PainStunTime = TimeSpan.FromSeconds(15);


    [ViewVariables(VVAccess.ReadOnly)]
    public byte GunshotsCount = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan EndThrowDuration = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public Vector2 TotalDirectionForce = Vector2.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? EndGunshotsTime;


    [ViewVariables(VVAccess.ReadWrite)]
    public byte? MeleeAttackSlowPercentage;

    [ViewVariables(VVAccess.ReadWrite)]
    public byte? RangeAttackSlowPercentage;
}
