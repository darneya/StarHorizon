using System.Numerics;
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Pain.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent]
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
