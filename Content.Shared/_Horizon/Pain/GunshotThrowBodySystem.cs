using System.Numerics;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Horizon.Pain;

/// <summary>
///
/// </summary>
public sealed class GunshotThrowBodySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = null!;
    [Dependency] private readonly SharedPhysicsSystem _physics = null!;
    [Dependency] private readonly SharedStunSystem _stun = null!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PainComponent, ProjectileTargetHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(Entity<PainComponent> pain, ref ProjectileTargetHitEvent ev)
    {
        if (pain.Comp.EndThrowDuration > _gameTiming.CurTime)
            return;
        if (!TryComp<PhysicsComponent>(ev.Projectile, out var physics))
            return;

        pain.Comp.EndGunshotsTime ??= _gameTiming.CurTime + pain.Comp.GunshotsTime;
        if (pain.Comp.EndGunshotsTime <= _gameTiming.CurTime)
        {
            pain.Comp.EndGunshotsTime = null;
            pain.Comp.GunshotsCount = 0;
            Dirty(pain);
            return;
        }

        pain.Comp.GunshotsCount++;
        pain.Comp.TotalDirectionForce += physics.LinearVelocity;
        if (pain.Comp.GunshotsCount >= pain.Comp.GunshotsToThrowBody)
        {
            TryThrowBody(pain.Comp.TotalDirectionForce.Normalized(), ev.Target);

            pain.Comp.EndThrowDuration = _gameTiming.CurTime + pain.Comp.ThrowDuration;
            pain.Comp.TotalDirectionForce = Vector2.Zero;
            pain.Comp.EndGunshotsTime = null;
            pain.Comp.GunshotsCount = 0;
        }

        Dirty(pain);
    }

    private void TryThrowBody(Vector2 normalized, EntityUid body)
    {
        _stun.TryKnockdown(body, TimeSpan.FromSeconds(2), false);
        _physics.SetLinearVelocity(body, normalized * 5f);
    }
}
