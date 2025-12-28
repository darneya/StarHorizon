using System.Linq;
using Content.Server._Horizon.Planet;
using Content.Server._NF.Cargo.Systems;
using Content.Server.Access.Systems;
using Content.Server.Cargo.Systems;
using Content.Server.CartridgeLoader;
using Content.Shared._Horizon.Expeditions;
using Content.Shared.Access.Components;
using Content.Shared.Cargo;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;
using Content.Shared.Tag;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
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
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    private Dictionary<GoalSpecification, Dictionary<int, ExpeditionGoal>> _goals = new();
    private Dictionary<int, ExpeditionGoal> _claimedGoals = new();
    private int _nextId = 1;
    private TimeSpan _nextOffer;

    public TimeSpan Cooldown = TimeSpan.FromMinutes(5);

    public const int GoalsCount = 3;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpeditionGoalsConsoleComponent, MapInitEvent>(OnConsoleInit);
        SubscribeLocalEvent<ExpeditionGoalsConsoleComponent, ClaimExpeditionGoalMessage>(OnClaim);

        SubscribeLocalEvent<GoalsListCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<GoalsListCartridgeComponent, CartridgeUiMessage>(OnCartridgeMessage);

        SubscribeLocalEvent<SpawnExpeditionGoalEntityEvent>(OnSpawnEntities);
        SubscribeLocalEvent<PriceCalculationEvent>(GetPrice);
        SubscribeLocalEvent<EntitySoldEvent>(OnSold);
        SubscribeLocalEvent<NFEntitySoldEvent>(OnSold);
    }

    private void OnConsoleInit(Entity<ExpeditionGoalsConsoleComponent> ent, ref MapInitEvent args)
    {
        UpdateUi(ent.Owner);
    }

    private void OnClaim(Entity<ExpeditionGoalsConsoleComponent> ent, ref ClaimExpeditionGoalMessage args)
    {
        if (!_idCard.TryFindIdCard(args.Actor, out var idCard))
            return;

        TryClaimGoal(idCard.Owner, args.OptionId, args.Specification);
    }

    private void OnUiReady(Entity<GoalsListCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        Dictionary<int, ExpeditionGoal> goals = new();

        if (TryComp<PdaComponent>(args.Loader, out var pda) &&
            pda.IdSlot?.ContainerSlot?.ContainedEntity is { Valid: true } card &&
            TryComp<ExpeditionGoalsIdCardComponent>(card, out var goalCard))
            goals = goalCard.AssignedGoals.Select(x => new KeyValuePair<int, ExpeditionGoal>(x, _claimedGoals[x])).ToDictionary();

        var state = new GoalsListCartridgeUiState(goals);
        _cartridgeLoader.UpdateCartridgeUiState(args.Loader, state);
    }

    private void OnCartridgeMessage(Entity<GoalsListCartridgeComponent> ent, ref CartridgeUiMessage args)
    {
        if (args.MessageEvent is not GoalsListRemoveMessage cast)
            return;

        _claimedGoals.Remove(cast.Id);
        _cartridgeLoader.UpdateUiState(GetEntity(args.MessageEvent.LoaderUid), null, null);
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
            if (!item.TryComplete(args.Entity, EntityManager))
                continue;

            if (args.Currency != item.RequiredStack)
                continue;

            if (item.IsContraband)
                continue;

            args.Price = item.Reward;
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
            foreach (var item in goalsCard.AssignedGoals.ToList())
            {
                if (!_claimedGoals.TryGetValue(item, out var goal))
                    continue;

                if (goal.IsContraband)
                    continue;

                if (!goal.TryComplete(sold, EntityManager))
                    continue;

                goalsCard.AssignedGoals.Remove(item);
            }

            Dirty(idCard.Owner, goalsCard);
        }
    }

    private void OnSold(ref NFEntitySoldEvent args)
    {
        if (!_idCard.TryFindIdCard(args.Actor, out var idCard) || !TryComp<ExpeditionGoalsIdCardComponent>(idCard.Owner, out var goalsCard))
            return;

        foreach (var sold in args.Sold)
        {
            foreach (var item in goalsCard.AssignedGoals.ToList())
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

    public int GetContrabandBonus(EntityUid actor, EntityUid ent, string currency)
    {
        if (!_idCard.TryFindIdCard(actor, out var idCard) || !TryComp<ExpeditionGoalsIdCardComponent>(idCard.Owner, out var goalsCard))
            return 0;

        foreach (var item in goalsCard.AssignedGoals)
        {
            if (!_claimedGoals.TryGetValue(item, out var goal))
                continue;

            if (!goal.IsContraband)
                continue;

            if (goal.RequiredStack != currency)
                continue;

            if (goal.TryComplete(ent, EntityManager))
                return goal.Reward;
        }

        return 0;
    }

    private void ClaimGoal(EntityUid idCard, int goalId, GoalSpecification specification)
    {
        if (!_goals[specification].TryGetValue(goalId, out var goal))
            return;

        if (goal.ClaimEvent != null)
            RaiseLocalEvent(goal.ClaimEvent);

        var card = EnsureComp<ExpeditionGoalsIdCardComponent>(idCard);
        card.AssignedGoals.Add(goalId);
        Dirty(idCard, card);

        _claimedGoals[goalId] = goal;

        GenerateGoals();
        UpdateUi();

        if (_container.TryGetContainingContainer(idCard, out var container))
            _cartridgeLoader.UpdateUiState(container.Owner, null, null);
    }

    private bool TryClaimGoal(EntityUid idCard, int goalId, GoalSpecification specification)
    {
        if (!_goals[specification].TryGetValue(goalId, out var goal))
            return false;

        if (TryComp<ExpeditionGoalsIdCardComponent>(idCard, out var card) && card.AssignedGoals.Count >= card.MaxGoals)
            return false;

        ClaimGoal(idCard, goalId, specification);
        return true;
    }

    public bool IsCompleted(EntityUid user, EntityUid target)
    {
        if (!_idCard.TryFindIdCard(user, out var idCard) || !TryComp<ExpeditionGoalsIdCardComponent>(idCard.Owner, out var goalsCard))
            return false;

        foreach (var item in goalsCard.AssignedGoals)
        {
            if (!_claimedGoals.TryGetValue(item, out var goal))
                continue;

            if (goal.TryComplete(target, EntityManager))
                return true;
        }

        return false;
    }

    private void GenerateGoals()
    {
        _goals.Clear();
        _nextOffer = _timing.CurTime + Cooldown;

        var prototypes = _proto.EnumeratePrototypes<ExpeditionGoalPrototype>().ToList();

        for (var j = 0; j <= (int)GoalSpecification.Pirates; j++)
        {
            var specification = (GoalSpecification)j;
            _goals[specification] = new();
            var specificated = prototypes.Where(x => x.Specification == specification).ToList();
            if (specificated.Count <= 0)
                continue;

            for (var i = 0; i < GoalsCount; i++)
            {
                var proto = _random.Pick(specificated);
                var goal = proto.Goal.Instantiate(proto.RandomAmount.Next(_random) * proto.AmountMultiplier);
                _goals[specification].Add(_nextId, goal);
                _nextId++;
            }
        }

    }

    private void UpdateUi(EntityUid uid)
    {
        if (!TryComp<ExpeditionGoalsConsoleComponent>(uid, out var console))
            return;

        _ui.SetUiState(uid, ExpeditionGoalsConsoleUiKey.Key,
            new ExpeditionGoalsConsoleUiState(_goals, console.Categories, Cooldown, _nextOffer));
    }

    private void UpdateUi()
    {
        var query = EntityQueryEnumerator<ExpeditionGoalsConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            _ui.SetUiState(uid, ExpeditionGoalsConsoleUiKey.Key,
                new ExpeditionGoalsConsoleUiState(_goals, console.Categories, Cooldown, _nextOffer));
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
