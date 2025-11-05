using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Expeditions;

[Prototype]
public sealed partial class ExpeditionGoalPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; set; } = default!;

    [DataField(required: true)]
    public ExpeditionGoal Goal = default!;
}

[Serializable, NetSerializable]
[ImplicitDataDefinitionForInheritors]
public abstract partial class ExpeditionGoal
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Description = default!;

    [DataField(serverOnly: true), ViewVariables(VVAccess.ReadWrite)]
    public object? ClaimEvent;

    public abstract ExpeditionGoal Instantiate(IRobustRandom random);
}

public sealed partial class EntityExpeditionGoal : ExpeditionGoal
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string RequiredTag = default!;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequireId = false;

    [DataField]
    public MinMax RandomAmount;

    [ViewVariables(VVAccess.ReadWrite)]
    public int RequiredAmount;

    public override ExpeditionGoal Instantiate(IRobustRandom random)
    {
        var amount = RandomAmount.Next(random);

        return new EntityExpeditionGoal()
        {
            Description = Loc.GetString(Description, ("amount", amount)),
            RequiredTag = RequiredTag,
            RequiredAmount = amount,
            RequireId = RequireId
        };
    }
}
