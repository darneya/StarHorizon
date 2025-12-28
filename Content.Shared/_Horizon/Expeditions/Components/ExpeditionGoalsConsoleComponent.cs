using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Expeditions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExpeditionGoalsConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<GoalSpecification> Categories = new()
    {
        GoalSpecification.Crew,
        GoalSpecification.Expeditionary,
        GoalSpecification.Mining,
        GoalSpecification.Medical
    };
}
