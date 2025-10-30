namespace Content.Shared._Horizon.Shipyard;

[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseVesselCostModifier
{
    [DataField(required: true)]
    protected float CostMultiplier = 1.0f;

    public abstract void Modify(EntityUid? user, ref int cost, IEntityManager entMan);
}
