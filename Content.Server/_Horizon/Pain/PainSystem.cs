using System.Numerics;
using Content.Shared._Horizon.Pain.Components;
using Content.Server.Chat.Systems;
using Content.Shared._Horizon.Medical.Damage;
using Content.Shared._Horizon.Pain.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.Pain;

public sealed class PainSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = null!;
    [Dependency] private readonly StandingStateSystem _standSystem = null!;
    [Dependency] private readonly SharedPhysicsSystem _physics = null!;
    [Dependency] private readonly IPrototypeManager _protoMan = null!;
    [Dependency] private readonly IGameTiming _gameTiming = null!;
    [Dependency] private readonly SharedStunSystem _stun = null!;
    [Dependency] private readonly IRobustRandom _random = null!;
    [Dependency] private readonly ChatSystem _chat = null!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileHitEffectComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<PainComponent, DamageBeforeApplyEvent>(OnDamageCause);
        SubscribeLocalEvent<PainComponent, RefreshMovementSpeedModifiersEvent>(SlowdownBody);
    }

    #region Projectiles
    private void OnProjectileHit(Entity<ProjectileHitEffectComponent> entity, ref ProjectileHitEvent ev)
    {
        if (!TryComp<PainComponent>(ev.Target, out var pain)
            || !TryComp<PhysicsComponent>(entity.Owner, out var physics))
            return;

        if (pain.EffectCooldown > _gameTiming.CurTime && pain.GunshotsCount.ContainsKey(entity.Comp.BulletId))
            return;

        if (pain.EndGunshotsTime <= _gameTiming.CurTime)
            ResetGunshots(pain, entity.Comp.BulletId);

        CountShot(ev.Target, pain, physics.LinearVelocity, entity.Comp.BulletId, ev.Damage.GetTotal());
        TryApplyGunshotsEffect(ev.Target, pain, entity.Comp);
    }

    private void CountShot(EntityUid body, PainComponent pain, Vector2 linearVelocity, string bulletId, FixedPoint2 totalDamage)
    {
        pain.EndGunshotsTime ??= _gameTiming.CurTime + TimeSpan.FromMilliseconds(500);
        if (pain.EffectCooldown > _gameTiming.CurTime)
            return;

        pain.TotalDamage += totalDamage;
        pain.TotalImpulse += linearVelocity;
        if (!pain.GunshotsCount.TryAdd(bulletId, 1))
            pain.GunshotsCount[bulletId]++;
    }

    private void ResetGunshots(PainComponent pain, string bulletId)
    {
        pain.TotalImpulse = Vector2.Zero;
        pain.TotalDamage = FixedPoint2.Zero;
        pain.GunshotsCount.Remove(bulletId);
        pain.EndGunshotsTime = null;
    }

    private void TryApplyGunshotsEffect(EntityUid target, PainComponent pain, ProjectileHitEffectComponent projectile)
    {
        if (!pain.GunshotsCount.TryGetValue(projectile.BulletId, out var shots))
            return;

        if (shots < projectile.Gunshots || pain.EndGunshotsTime <= _gameTiming.CurTime)
            return;

        if (projectile.Push)
            _physics.ApplyLinearImpulse(target, pain.TotalImpulse * 10f);

        _stun.TryStun(target, projectile.EffectDuration, true);
        TryCauseScreamOfPain(target, pain.ScreamOfPainPrototype, pain.TotalDamage, ref pain.NextPossibleScream);
        pain.EffectCooldown = _gameTiming.CurTime + projectile.EffectCooldown;
    }
    #endregion

    private void OnDamageCause(Entity<PainComponent> entity, ref DamageBeforeApplyEvent ev)
    {
        if (ev.Cancelled || ev.Damage.Empty || HasComp<PainNumbnessComponent>(entity.Owner))
            return;

        if (CheckPainRequirements(entity))
            return;

        if (ev.Damage.GetTotal() > 0)
            AdjustPainDamage(entity, entity.Comp, ev.Damage);
        else
            RemovePainDamage(entity, entity.Comp, ev.Damage.DamageDict);
    }

    private bool CheckPainRequirements(EntityUid uid)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable)
            || !TryComp<MobThresholdsComponent>(uid, out var thresholds))
            return true;

        var total = damageable.Damage.GetTotal();
        foreach (var (damage, state) in thresholds.Thresholds)
        {
            if (total > damage && state is MobState.Critical or MobState.Invalid)
                return true;
        }

        return false;
    }

    public void AdjustPainDamage(EntityUid body, PainComponent pain, DamageSpecifier specifier)
    {
        var converter = pain.DamagePrototypeConverter;
        if (!_protoMan.TryIndex<PainConverterPrototype>(converter, out var proto))
            return;

        foreach (var (type, damage) in specifier.DamageDict)
        {
            if (!proto.PainPerDamage.TryGetValue(type, out var painPerDamage))
                continue;

            pain.CurrentPain += painPerDamage.Float() * damage.Float();
        }

        TryCauseScreamOfPain(body, pain.ScreamOfPainPrototype, specifier.GetTotal(), ref pain.NextPossibleScream);
        UpdatePainStage(body, ref pain, pain.CurrentPain, pain.PainThresholds);
        if (pain.CurrentPain > pain.PainThresholds[PainStages.DeadPain])
            pain.CurrentPain = pain.PainThresholds[PainStages.DeadPain];
    }

    public void RemovePainDamage(EntityUid body, PainComponent pain, Dictionary<string, FixedPoint2> damageDict)
    {
        var converter = pain.DamagePrototypeConverter;
        if (!_protoMan.TryIndex<PainConverterPrototype>(converter, out var proto))
            return;

        foreach (var (type, damage) in damageDict)
        {
            if (!proto.PainPerDamage.TryGetValue(type, out var painPerDamage))
                continue;

            pain.CurrentPain += painPerDamage.Float() * damage.Float();
        }

        UpdatePainStage(body, ref pain, pain.CurrentPain, pain.PainThresholds);
        if (pain.CurrentPain < pain.PainThresholds[PainStages.Nothing])
            pain.CurrentPain = pain.PainThresholds[PainStages.Nothing];
    }

    private void UpdatePainStage(EntityUid body, ref PainComponent comp, float pain, SortedDictionary<PainStages, float> painThresholds)
    {
        var actualStage = PainStages.Nothing;
        foreach (var (stage, painLevel) in painThresholds)
        {
            if (pain >= painLevel)
                actualStage = stage;
        }

        comp.CurrentStage = actualStage;
        TryDownBody(body, comp.CurrentStage);
        _movement.RefreshMovementSpeedModifiers(body);
        DirtyField(body, comp, nameof(PainComponent.CurrentStage));
    }

    private void SlowdownBody(EntityUid body, PainComponent pain, ref RefreshMovementSpeedModifiersEvent ev)
    {
        if (pain.CurrentStage <= PainStages.AveragePain)
            return;

        if (!TryComp<DamageableComponent>(body, out var damageable))
            return;

        var total = damageable.Damage.GetTotal();
        if (total == 0)
            return;

        var heat = damageable.Damage.DamageDict["Heat"];
        var percentage = Math.Clamp(1f - (heat / total).Float(), 0.4f, 1f);
        if (_standSystem.IsDown(body))
            ev.ModifySpeed(percentage * 0.4f);
        ev.ModifySpeed(percentage);
    }

    private void TryDownBody(EntityUid body, PainStages currentStage)
    {
        if (currentStage != PainStages.UnbeatablePain)
            return;

        if (_standSystem.IsDown(body))
            return;

        _standSystem.Down(body);
        _movement.RefreshMovementSpeedModifiers(body);
    }

    private void TryCauseScreamOfPain(EntityUid body, string screamList, FixedPoint2 pain, ref TimeSpan nextPossibleScream)
    {
        if (_gameTiming.CurTime < nextPossibleScream)
            return;

        var screamProto = _protoMan.Index<ScreamOfPainPrototype>(screamList);
        string? scream = null;
        foreach (var (damage, list) in screamProto.ScreamList)
        {
            if (pain >= damage)
                scream = list[_random.Next(0, list.Count)];
        }

        if (scream is null)
            return;

        _chat.TrySendInGameICMessage(body, scream, InGameICChatType.Speak, false, true);
        nextPossibleScream = _gameTiming.CurTime + TimeSpan.FromSeconds(1);
    }
}
