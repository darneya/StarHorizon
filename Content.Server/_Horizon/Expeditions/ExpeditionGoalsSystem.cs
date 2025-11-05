using System.Linq;
using Content.Shared.Access.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Horizon.Expeditions;

public sealed class ExpeditionGoalsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private Dictionary<int, ExpeditionGoal> _goals = new();
    private int _nextId = 1;

    public const int GoalsCount = 5;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpeditionGoalsConsoleComponent, MapInitEvent>(OnConsoleInit);
    }

    private void OnConsoleInit(Entity<ExpeditionGoalsConsoleComponent> ent, ref MapInitEvent args)
    {
        UpdateUi(ent.Owner);
    }

    private void ClaimGoal(EntityUid idCard, int goalId)
    {
        if (!_goals.TryGetValue(goalId, out var goal))
            return;

        if (goal.ClaimEvent != null)
            RaiseLocalEvent(idCard, goal.ClaimEvent);

        GenerateGoals();
        UpdateUi();
    }

    private void GenerateGoals()
    {
        _goals.Clear();

        var prototypes = _proto.EnumeratePrototypes<ExpeditionGoalPrototype>().ToList();

        for (var i = 0; i < GoalsCount; i++)
        {
            var proto = _random.PickAndTake(prototypes);
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
            new ExpeditionGoalsConsoleUiState(_goals));
    }

    private void UpdateUi()
    {
        var query = EntityQueryEnumerator<ExpeditionGoalsConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            _ui.SetUiState(uid, ExpeditionGoalsConsoleUiKey.Key,
                new ExpeditionGoalsConsoleUiState(_goals));
        }
    }
}
