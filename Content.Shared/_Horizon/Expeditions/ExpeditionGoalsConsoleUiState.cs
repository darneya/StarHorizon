using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Expeditions;

[Serializable, NetSerializable]
public sealed partial class ExpeditionGoalsConsoleUiState : BoundUserInterfaceState
{
    public Dictionary<int, ExpeditionGoal> Goals;

    public ExpeditionGoalsConsoleUiState(Dictionary<int, ExpeditionGoal> goals)
    {
        Goals = goals;
    }
}

[Serializable, NetSerializable]
public enum ExpeditionGoalsConsoleUiKey
{
    Key,
}
