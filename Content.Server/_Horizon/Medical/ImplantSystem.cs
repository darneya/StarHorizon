using Content.Shared._Horizon.Medical.Surgery.Components;
using Content.Shared._Horizon.Medical.Surgery.Events;

namespace Content.Server._Horizon.Medical;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
public sealed class ImplantSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganImplantComponent, SurgeryOrganImplantationCompleted>(OnOrganInsertComplete);

        SubscribeLocalEvent<OrganImplantComponent, SurgeryOrganExtractCompleted>(OnOrganExtractComplete);
    }

    private void OnOrganInsertComplete(Entity<OrganImplantComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        foreach (var comp in (ent.Comp.AddComp ?? []).Values)
        {
            if (!EntityManager.HasComponent(args.Body, comp.Component.GetType()))
                EntityManager.AddComponent(args.Body, _compFactory.GetComponent(comp.Component.GetType()));
        }
    }

    private void OnOrganExtractComplete(Entity<OrganImplantComponent> ent, ref SurgeryOrganExtractCompleted args)
    {
        foreach (var comp in (ent.Comp.AddComp ?? []).Values)
        {
            if (EntityManager.HasComponent(args.Body, comp.Component.GetType()))
                EntityManager.RemoveComponent(args.Body, _compFactory.GetComponent(comp.Component.GetType()));
        }
    }
}
