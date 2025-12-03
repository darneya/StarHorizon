using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Expeditions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ExpeditionGoalsIdCardComponent : Component
{
    [AutoNetworkedField]
    public Dictionary<int, ClaimedExpeditionGoal> AssignedGoals = new();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxGoals = 2;
}

[Serializable, NetSerializable]
public sealed class ClaimedExpeditionGoal
{
    [ViewVariables(VVAccess.ReadWrite)]
    public string Description = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public string IconEntity = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public int Reward = default!;

    public ClaimedExpeditionGoal(string description, string iconEntity, int reward)
    {
        Description = description;
        IconEntity = iconEntity;
        Reward = reward;
    }
}
