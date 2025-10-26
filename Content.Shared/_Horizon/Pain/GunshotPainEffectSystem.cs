using System.Numerics;
using Content.Shared._Horizon.Pain.Components;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Horizon.Pain;

/// <summary>
/// Вызывает новые эффекты при попадании пулями
/// </summary>
public sealed class GunshotPainEffectSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = null!;
    [Dependency] private readonly SharedPhysicsSystem _physics = null!;
    [Dependency] private readonly SharedStunSystem _stun = null!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ProjectileHitEffectComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(Entity<ProjectileHitEffectComponent> entity, ref ProjectileHitEvent ev)
    {
        if (!TryComp<GunshotPainEffectedComponent>(ev.Target, out var painBody)
            || !TryComp<PhysicsComponent>(entity.Owner, out var physics))
            return;

        if (painBody.EffectCooldown > _gameTiming.CurTime && painBody.GunshotsCount.ContainsKey(entity.Comp.BulletId))
            return;

        CountShot(painBody, physics.LinearVelocity, entity.Comp.BulletId);
        TryApplyGunshotsEffect(ev.Target, painBody, entity.Comp);
        Dirty(ev.Target, painBody);
    }

    private void CountShot(GunshotPainEffectedComponent component, Vector2 linearVelocity, string bulletId)
    {
        component.EndGunshotsTime ??= _gameTiming.CurTime + TimeSpan.FromMilliseconds(500);
        if (component.EffectCooldown > _gameTiming.CurTime)
            return;

        component.TotalImpulse += linearVelocity;
        if (!component.GunshotsCount.TryAdd(bulletId, 1))
            component.GunshotsCount[bulletId]++;
    }

    private bool ResetGunshots(GunshotPainEffectedComponent component, string bulletId)
    {
        if (component.EndGunshotsTime > _gameTiming.CurTime)
            return false;

        component.GunshotsCount.Remove(bulletId);
        component.EndGunshotsTime = null;
        return true;
    }

    private void TryApplyGunshotsEffect(EntityUid target, GunshotPainEffectedComponent component, ProjectileHitEffectComponent projectile)
    {
        if (component.GunshotsCount[projectile.BulletId] < projectile.GunshotsToApplyEffect && ResetGunshots(component, projectile.BulletId))
            return;

        if (projectile.Push)
            _physics.ApplyLinearImpulse(target, component.TotalImpulse);

        switch (projectile.Effect)
        {
            case "KnockedDown":
                _stun.TryParalyze(target, projectile.EffectDuration, true);
                break;

            case "Stun":
                _stun.TryStun(target, projectile.EffectDuration, true);
                break;

            case "SlowedDown":
                _stun.TrySlowdown(target, projectile.EffectDuration, true, projectile.SlowdownPower, projectile.SlowdownPower);
                break;
        }
        component.EffectCooldown = _gameTiming.CurTime + projectile.EffectCooldown;
        component.TotalImpulse = Vector2.Zero;
    }
}
