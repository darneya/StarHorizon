using Content.Shared._Horizon.Cytology.Systems;
using Content.Shared.Verbs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components.SolutionManager;
using System.Runtime.CompilerServices;
using Dependency = Robust.Shared.IoC.DependencyAttribute;
using Content.Shared._Horizon.Cytology.Components;
using Robust.Shared.Prototypes;
using Content.Shared._Horizon.Cytology.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Shared._Horizon.Cytology;
using Content.Shared._Horizon.Cytology.Components;
using Content.Shared._Horizon.Cytology.Prototypes;
using Content.Shared._Horizon.Cytology.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Lathe;
using Content.Shared.Power;
using Content.Shared.Research.Prototypes;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Runtime.CompilerServices;
using Content.Shared._Horizon.Cytology;
using Robust.Shared.Maths;
using Content.Shared.Lathe;
using Content.Shared.Power;
using Content.Shared.Research.Prototypes;

namespace Content.Server._Horizon.Cytology;

public sealed class GrowingVatSystem : SharedGrowingVatSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CytologyGrowingVatComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerb);
    }

    private void OnGetVerb(Entity<CytologyGrowingVatComponent> growingVat, ref GetVerbsEvent<InteractionVerb> args) //TODO Сделать предиктед
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract || args.Hands == null)
            return;

        InteractionVerb verb = new()
        {
            Act = growingVat.Comp.IsActive
                ? () => ToggleOff(growingVat)
                : () => ToggleOn(growingVat),
            Text = Loc.GetString("verb-toggle-growing-vat")
        };

        args.Verbs.Add(verb);

    }

    private void ToggleOn(Entity<CytologyGrowingVatComponent> growingVat)
    {
        growingVat.Comp.IsActive = true;
        DirtyField(growingVat.Owner, growingVat.Comp, nameof(growingVat.Comp.IsActive));
        Appearance.SetData(growingVat.Owner, CytologyGrowingVatVisualStates.Working, true);
    }

    private void ToggleOff(Entity<CytologyGrowingVatComponent> growingVat)
    {
        growingVat.Comp.IsActive = false;
        DirtyField(growingVat.Owner, growingVat.Comp, nameof(growingVat.Comp.IsActive));
        Appearance.SetData(growingVat.Owner, CytologyGrowingVatVisualStates.Working, false);
    }
}
