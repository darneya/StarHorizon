using System.Linq;
using Content.Server._Horizon.Planet;
using Content.Server.Access.Systems;
using Content.Server.Cargo.Systems;
using Content.Shared._Horizon.Expeditions;
using Content.Shared.Access.Components;
using Content.Shared.Cargo;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.Expeditions;

public sealed class ExpeditionGoalsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly PlanetSystem _planet = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;

    private Dictionary<int, ExpeditionGoal> _goals = new();
    private Dictionary<int, KeyValuePair<EntityUid, ExpeditionGoal>> _claimedGoals = new();
    private int _nextId = 1;
    private TimeSpan _nextOffer;

    public TimeSpan Cooldown = TimeSpan.FromMinutes(5);

    public const int GoalsCount = 5;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpeditionGoalsConsoleComponent, MapInitEvent>(OnConsoleInit);
        SubscribeLocalEvent<ExpeditionGoalsConsoleComponent, ClaimExpeditionGoalMessage>(OnClaim);

        SubscribeLocalEvent<SpawnExpeditionGoalEntityEvent>(OnSpawnEntities);
        SubscribeLocalEvent<PriceCalculationEvent>(GetPrice);
        SubscribeLocalEvent<EntitySoldEvent>(OnSold);
    }

    private void OnConsoleInit(Entity<ExpeditionGoalsConsoleComponent> ent, ref MapInitEvent args)
    {
        UpdateUi(ent.Owner);
    }

    private void OnClaim(Entity<ExpeditionGoalsConsoleComponent> ent, ref ClaimExpeditionGoalMessage args)
    {
        if (!_idCard.TryFindIdCard(args.Actor, out var idCard))
            return;

        TryClaimGoal(idCard.Owner, args.OptionId);
    }

    private void OnSpawnEntities(SpawnExpeditionGoalEntityEvent args)
    {
        if (!_planet.LoadedPlanets.TryGetValue(args.Planet, out var planetUid))
        {
            Log.Warning("Tried to spawn expedition goal target on non-exsisting planet.");
            return;
        }

        // getting all markers
        var markers = EntityManager.AllEntities<TagComponent>().Where(x => _tag.HasTag(x.Owner, args.SpawnerTag) && Transform(x).Coordinates.EntityId == planetUid).ToList();
        _random.Shuffle(markers);

        if (markers.Count <= 0)
        {
            Log.Warning("Tried to spawn expedition goal target without having markers.");
            return;
        }


        for (var i = 0; i < markers.Count && i < args.MarkersCount; i++)
        {
            var markerCoords = Transform(markers[i]).Coordinates;

            for (var e = 0; e < args.SpawnsPerMarker; e++)
            {
                var ent = Spawn(_random.Pick(args.SpawnedEntities), markerCoords);
            }
        }
    }

    private void GetPrice(ref PriceCalculationEvent args)
    {
        foreach (var item in _claimedGoals.Values)
        {
            if (!item.Value.TryComplete(args.Entity, EntityManager))
                continue;

            args.Price = item.Value.Reward;
            args.Handled = true;
            return;
        }
    }

    private void OnSold(ref EntitySoldEvent args)
    {
        if (!_idCard.TryFindIdCard(args.Actor, out var idCard) || !TryComp<ExpeditionGoalsIdCardComponent>(idCard.Owner, out var goalsCard))
            return;

        foreach (var sold in args.Sold)
        {
            foreach (var item in goalsCard.AssignedGoals)
            {
                if (!_claimedGoals.TryGetValue(item, out var goal))
                    continue;

                if (!goal.TryComplete(sold, EntityManager))
                    continue;

                goalsCard.AssignedGoals.Remove(item);
            }

            Dirty(idCard.Owner, goalsCard);
        }
    }

    private void ClaimGoal(EntityUid idCard, int goalId)
    {
        if (!_goals.TryGetValue(goalId, out var goal))
            return;

        if (goal.ClaimEvent != null)
            RaiseLocalEvent(goal.ClaimEvent);

        var card = EnsureComp<ExpeditionGoalsIdCardComponent>(idCard);
        card.AssignedGoals[goalId] = new(goal.Description, goal.IconEntity, goal.Reward);
        Dirty(idCard, card);

        _claimedGoals[goalId] = new(idCard, goal);

        GenerateGoals();
        UpdateUi();
    }

    private bool TryClaimGoal(EntityUid idCard, int goalId)
    {
        if (!_goals.TryGetValue(goalId, out var goal))
            return false;

        if (TryComp<ExpeditionGoalsIdCardComponent>(idCard, out var card) && card.AssignedGoals.Count >= card.MaxGoals)
            return false;

        ClaimGoal(idCard, goalId);
        return true;
    }

    public bool IsCompleted(EntityUid user, EntityUid target)
    {
        _idCard.TryFindIdCard(user, out var card);
        foreach (var item in _claimedGoals.Values)
        {
            if (item.Key != card.Owner)
                continue;

            if (item.Value.TryComplete(target, EntityManager))
                return true;
        }

        return false;
    }

    private void GenerateGoals()
    {
        _goals.Clear();
        _nextOffer = _timing.CurTime + Cooldown;

        var prototypes = _proto.EnumeratePrototypes<ExpeditionGoalPrototype>().ToList();

        for (var i = 0; i < GoalsCount; i++)
        {
            var proto = _random.Pick(prototypes);
            var goal = proto.Goal.Instantiate(_random);
            _goals.Add(_nextId, goal);
            _nextId++;
        }
    }

    private void UpdateUi(EntityUid uid)
    {
        if (!HasComp<ExpeditionGoalsConsoleComponent>(uid))
            return;

        _ui.SetUiState(uid, ExpeditionGoalsConsoleUiKey.Key,
            new ExpeditionGoalsConsoleUiState(_goals, Cooldown, _nextOffer));
    }

    private void UpdateUi()
    {
        var query = EntityQueryEnumerator<ExpeditionGoalsConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            _ui.SetUiState(uid, ExpeditionGoalsConsoleUiKey.Key,
                new ExpeditionGoalsConsoleUiState(_goals, Cooldown, _nextOffer));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextOffer)
            return;

        GenerateGoals();
        UpdateUi();
    }
}
