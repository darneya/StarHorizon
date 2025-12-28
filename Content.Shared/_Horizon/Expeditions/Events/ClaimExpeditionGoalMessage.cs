using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Expeditions;

[Serializable, NetSerializable]
public sealed partial class ClaimExpeditionGoalMessage : BoundUserInterfaceMessage
{
    public int OptionId;
    public GoalSpecification Specification;

    public ClaimExpeditionGoalMessage(int optionId, GoalSpecification specification)
    {
        OptionId = optionId;
        Specification = specification;
    }
}
