using Content.Shared.Destructible.Thresholds;
using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using Content.Shared.Tag;
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

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string IconEntity = default!;

    [DataField(serverOnly: true), ViewVariables(VVAccess.ReadWrite)]
    public object? ClaimEvent;

    public abstract ExpeditionGoal Instantiate(IRobustRandom random);

    public abstract bool TryComplete(EntityUid sellEntity, IEntityManager entMan);
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

    public override bool TryComplete(EntityUid sellEntity, IEntityManager entMan)
    {
        int count = 0;

        IncreaseFromStack(sellEntity, ref count, entMan);

        if (entMan.TryGetComponent<SharedEntityStorageComponent>(sellEntity, out var storage))
        {
            foreach (var item in storage.Contents.ContainedEntities)
                IncreaseFromStack(item, ref count, entMan);
        }

        return count >= RequiredAmount;
    }

    private void IncreaseFromStack(EntityUid sellEntity, ref int count, IEntityManager entMan)
    {
        var tagSys = entMan.System<TagSystem>();

        if (tagSys.HasTag(sellEntity, RequiredTag))
            count += entMan.GetComponentOrNull<StackComponent>(sellEntity)?.Count ?? 1;
    }
}
