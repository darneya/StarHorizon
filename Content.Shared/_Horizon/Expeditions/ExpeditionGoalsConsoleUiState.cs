using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Expeditions;

[Serializable, NetSerializable]
public sealed partial class ExpeditionGoalsConsoleUiState : BoundUserInterfaceState
{
    public Dictionary<GoalSpecification, Dictionary<int, ExpeditionGoal>> Goals;
    public List<GoalSpecification> AvailableSpecifications;
    public TimeSpan OfferCooldown;
    public TimeSpan Cooldown;

    public ExpeditionGoalsConsoleUiState(Dictionary<GoalSpecification, Dictionary<int, ExpeditionGoal>> goals, List<GoalSpecification> availableSpecifications, TimeSpan cooldown, TimeSpan offerCooldown)
    {
        Goals = goals;
        AvailableSpecifications = availableSpecifications;
        Cooldown = cooldown;
        OfferCooldown = offerCooldown;
    }
}

[Serializable, NetSerializable]
public enum ExpeditionGoalsConsoleUiKey
{
    Key,
}
