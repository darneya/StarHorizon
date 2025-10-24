using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Pain;

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


    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public byte GunshotsToThrowBody = 6;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ThrowDuration = TimeSpan.FromSeconds(30);

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan GunshotsTime = TimeSpan.FromSeconds(0.5);

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
