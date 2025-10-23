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
    public TimeSpan? EndThrowDuration;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? EndGunshotsTime;


    [ViewVariables(VVAccess.ReadWrite)]
    public byte? MeleAttackSlowPercentage;

    [ViewVariables(VVAccess.ReadWrite)]
    public byte? RangeAttackSlowPercentage;
}
