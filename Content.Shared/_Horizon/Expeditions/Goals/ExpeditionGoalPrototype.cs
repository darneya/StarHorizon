using Content.Shared.Destructible.Thresholds;
using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
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

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public MinMax RandomAmount;

    [DataField]
    public int AmountMultiplier = 1;

    [DataField]
    public ProtoId<ExpeditionGoalCategoryPrototype> Specification = "Crew";

    [DataField(required: true)]
    public ExpeditionGoal Goal = default!;
}

[Serializable, NetSerializable]
[ImplicitDataDefinitionForInheritors]
public abstract partial class ExpeditionGoal
{
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string Description = default!;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string IconEntity = default!;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public int Reward = default!;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string CurrencyStr = "$";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string RequiredStack = "Credit";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsContraband = false;

    [DataField(serverOnly: true), ViewVariables(VVAccess.ReadWrite)]
    public object? ClaimEvent;

    public abstract ExpeditionGoal Instantiate(int amount);

    public abstract bool TryComplete(EntityUid sellEntity, IEntityManager entMan);
}

[Prototype]
public sealed partial class ExpeditionGoalCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; set; } = default!;
}

