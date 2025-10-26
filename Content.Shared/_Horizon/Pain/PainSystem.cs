using Content.Shared._Horizon.Medical.Damage;
using Content.Shared._Horizon.Pain.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Shared._Horizon.Pain;

/// <summary>
///
/// </summary>
public sealed class PainSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = null!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PainComponent, DamageBeforeApplyEvent>(OnDamageCause);
    }

    private void OnDamageCause(Entity<PainComponent> entity, ref DamageBeforeApplyEvent ev)
    {
        if (ev.Cancelled || ev.Damage.Empty)
            return;

        var pain = entity.Comp;
        var actualDmg = ev.Damage.DamageDict;
        var totalDmg = ev.Damage.GetTotal();

        pain.CurrentPain += TransferDmgToPain(actualDmg, pain.DamageToPainConv);
        pain.AdrenalinRush += pain.AdrenalinRushTime.Item1 * (totalDmg.Float() / pain.AdrenalinRushTime.Item2) + _gameTiming.CurTime;
        UpdatePainStages(pain);
    }

    private static float TransferDmgToPain(Dictionary<string, FixedPoint2> damageDict,
        Dictionary<string, float> damageToPainConv)
    {
        var pain = 0f;
        foreach (var (type, dmg) in damageDict)
        {
            if (!damageToPainConv.TryGetValue(type, out var painConv))
                continue;

            pain += painConv * dmg.Float();
        }

        return pain;
    }

    public override void Update(float frame)
    {
        base.Update(frame);

        var enumerator = EntityManager.EntityQueryEnumerator<PainComponent>();
        while (enumerator.MoveNext(out _, out var pain))
        {
            if (pain.AdrenalinRush <= _gameTiming.CurTime)
                UpdatePainStages(pain);

            if (pain.NextUpdate == TimeSpan.Zero)
                pain.NextUpdate = _gameTiming.CurTime + pain.UpdateTime;

            if (pain.NextUpdate > _gameTiming.CurTime)
                continue;

            pain.CurrentPain -= pain.ReducePainPerUpdate;
            if (pain.CurrentPain < 0)
                pain.CurrentPain = 0;

            UpdatePainStages(pain);
        }
    }

    private void UpdatePainStages(PainComponent pain)
    {
        if (pain.AdrenalinRush > _gameTiming.CurTime)
            return;

        foreach (var (stage, painOfStage) in pain.PainThresholds)
        {
            if (painOfStage <= pain.CurrentPain)
                pain.CurrentStage = stage;
        }

        UpdatePainEffects(pain.CurrentStage);
    }

    private static void UpdatePainEffects(PainStages pain)
    {
        //TODO: Сделать какие либо эффекты в зависимости от силы боли.
    }
}

public enum PainStages : byte
{
    Nothing = 0,
    MildPain = 1,
    AveragePain = 2,
    SeverePain = 3,
    UnbeatablePain = 4,
}
