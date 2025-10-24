using System.Numerics;
using Content.Shared._Horizon.Pain.Components;
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
        SubscribeLocalEvent<ThrowOnProjectileHitComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(Entity<ThrowOnProjectileHitComponent> entity, ref ProjectileHitEvent ev)
    {
        if (!TryComp<PainComponent>(ev.Target, out var pain) || !TryComp<PhysicsComponent>(entity.Owner, out var physics))
            return;

        if (pain.EndThrowDuration > _gameTiming.CurTime)
            return;

        pain.GunshotsCount++;
        pain.EndGunshotsTime ??= _gameTiming.CurTime + entity.Comp.GunshotsTime;
        if (pain.EndGunshotsTime <= _gameTiming.CurTime)
        {
            pain.EndGunshotsTime = null;
            pain.GunshotsCount = 0;
        }

        pain.TotalDirectionForce += physics.LinearVelocity;
        if (pain.GunshotsCount >= entity.Comp.GunshotsToThrowBody)
        {
            _physics.ApplyLinearImpulse(ev.Target, pain.TotalDirectionForce * entity.Comp.AdditionalForce);
            pain.EndThrowDuration = _gameTiming.CurTime + entity.Comp.ThrowDuration;
            pain.TotalDirectionForce = Vector2.Zero;
            pain.EndGunshotsTime = null;
            pain.GunshotsCount = 0;
        }

        Dirty(ev.Target, pain);
    }
}
