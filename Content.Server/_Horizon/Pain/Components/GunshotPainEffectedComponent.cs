using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Server._Horizon.Pain.Components;

/// <summary>
///
/// </summary>
[RegisterComponent]
public sealed partial class GunshotPainEffectedComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<string, byte> GunshotsCount = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan EffectCooldown = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public Vector2 TotalImpulse = Vector2.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? EndGunshotsTime = TimeSpan.FromMilliseconds(500);
}
