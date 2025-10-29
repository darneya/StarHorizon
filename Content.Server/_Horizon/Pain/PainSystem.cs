using Content.Server.Chat.Systems;
using Content.Shared._Horizon.Medical.Damage;
using Content.Server._Horizon.Pain.Components;
using Content.Shared._Horizon.Pain.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Horizon.Pain;

/// <summary>
///
/// </summary>
public sealed class PainSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = null!;
    [Dependency] private readonly StandingStateSystem _standSystem = null!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = null!;
    [Dependency] private readonly IPrototypeManager _protoMan = null!;
    [Dependency] private readonly IRobustRandom _random = null!;
    [Dependency] private readonly ChatSystem _chat = null!;

    private ISawmill _sawmill = null!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PainComponent, RefreshMovementSpeedModifiersEvent>(RefreshMovementSpeed);
        SubscribeLocalEvent<PainComponent, DamageBeforeApplyEvent>(OnDamageCause);
        SubscribeLocalEvent<PainComponent, PainEffectEvent>(ApplyPainEffect);

        _sawmill = Logger.GetSawmill("painLogger");
    }

    private void OnDamageCause(Entity<PainComponent> entity, ref DamageBeforeApplyEvent ev)
    {
        if (CheckPainRequirements(entity))
            return;

        if (ev.Cancelled || ev.Damage.Empty)
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

        TryCauseScreamOfPain(body, pain.ScreamOfPainPrototype, specifier.GetTotal());
        pain.CurrentStage = UpdatePainStage(body, pain.CurrentPain, pain.PainThresholds);
        if (pain.CurrentPain > pain.PainThresholds[PainStages.UnbeatablePain])
            pain.CurrentPain = pain.PainThresholds[PainStages.UnbeatablePain];
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

        pain.CurrentStage = UpdatePainStage(body, pain.CurrentPain, pain.PainThresholds);
        if (pain.CurrentPain < pain.PainThresholds[PainStages.Nothing])
            pain.CurrentPain = pain.PainThresholds[PainStages.Nothing];
    }

    private void ApplyPainEffect(EntityUid body, PainComponent pain, ref PainEffectEvent ev)
    {
        switch (ev.Key)
        {
            case "TryStandUp":
                if (pain.CurrentStage != PainStages.UnbeatablePain)
                    return;

                _popupSystem.PopupEntity(Loc.GetString("pain-standup-cancelled"), body, PopupType.LargeCaution);
                _sawmill.Debug($"Попытка встать была прервана из - за невыносимой боли у {MetaData(body).EntityName}");
                ev.Cancelled = true;
                break;

            default:
                break;
        }
    }

    private PainStages UpdatePainStage(EntityUid body, float pain, SortedDictionary<PainStages, float> painThresholds)
    {
        var actualStage = PainStages.Nothing;
        foreach (var (stage, painLevel) in painThresholds)
        {
            if (pain >= painLevel)
                actualStage = stage;
        }

        UpdatePainStageEffects(body, actualStage);
        _movement.RefreshMovementSpeedModifiers(body);

        return actualStage;
    }

    private void UpdatePainStageEffects(EntityUid body, PainStages stage)
    {
        switch (stage)
        {
            case PainStages.Nothing:

            case PainStages.MildPain:

            case PainStages.AveragePain:

            case PainStages.SeverePain:
                break;

            case PainStages.UnbeatablePain:
                if (!_standSystem.IsDown(body))
                    _standSystem.Down(body);
                break;

            default:
                break;
        }
    }

    private void RefreshMovementSpeed(EntityUid body, PainComponent pain, ref RefreshMovementSpeedModifiersEvent args)
    {
        switch (pain.CurrentStage)
        {
            case PainStages.Nothing:
                break;

            case PainStages.MildPain:
                args.ModifySpeed(0.9f);
                break;

            case PainStages.AveragePain:
                args.ModifySpeed(0.8f);
                break;

            case PainStages.SeverePain:
                args.ModifySpeed(0.6f);
                break;

            case PainStages.UnbeatablePain:
                args.ModifySpeed(0.4f);
                break;

            default:
                break;
        }
    }

    private void TryCauseScreamOfPain(EntityUid body, string screamList, FixedPoint2 pain)
    {
        var screamProto = _protoMan.Index<ScreamOfPainPrototype>(screamList);
        foreach (var (damage, list) in screamProto.ScreamList)
        {
            if (damage > pain)
                continue;

            var scream = list[_random.Next(0, list.Count)];
            _chat.TrySendInGameICMessage(body, scream, InGameICChatType.Speak, false, true);
            return;
        }
    }
}
