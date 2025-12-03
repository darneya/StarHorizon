using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Expeditions;

[Serializable, NetSerializable]
public sealed partial class ExpeditionGoalsConsoleUiState : BoundUserInterfaceState
{
    public Dictionary<int, ExpeditionGoal> Goals;
    public TimeSpan OfferCooldown;
    public TimeSpan Cooldown;

    public ExpeditionGoalsConsoleUiState(Dictionary<int, ExpeditionGoal> goals, TimeSpan cooldown, TimeSpan offerCooldown)
    {
        Goals = goals;
        Cooldown = cooldown;
        OfferCooldown = offerCooldown;
    }
}

[Serializable, NetSerializable]
public enum ExpeditionGoalsConsoleUiKey
{
    Key,
}
