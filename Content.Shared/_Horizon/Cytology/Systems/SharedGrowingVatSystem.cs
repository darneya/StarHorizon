using Content.Shared._Horizon.Cytology.Components;
using Content.Shared._Horizon.Cytology.Prototypes;
using Content.Shared._Horizon.Cytology.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Labels.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared._Horizon.Cytology.Components;
using Content.Shared.Verbs;
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
using Content.Shared.Power;

namespace Content.Shared._Horizon.Cytology.Systems;

public abstract class SharedGrowingVatSystem : EntitySystem
{

    public const string BeakerSlotName = "beakerSlot";
    public const string PetriDishSlotName = "petriDishSlot";


    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CytologyGrowingVatComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
        SubscribeLocalEvent<CytologyGrowingVatComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CytologyGrowingVatComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CytologyGrowingVatComponent>();
        while (query.MoveNext(out var uid, out var cytologyGrowingVatComp))
        {
            if (!cytologyGrowingVatComp.IsActive)
                continue;

            if (cytologyGrowingVatComp.NextUpdate > _timing.CurTime)
                continue;

            cytologyGrowingVatComp.NextUpdate = _timing.CurTime + cytologyGrowingVatComp.UpdateInterval;

            if (!TryGetSolutionFromBeaker(uid, out var beakerSolution))
                continue;

            var dishEnt = _itemSlotsSystem.GetItemOrNull(uid, PetriDishSlotName);
            if (dishEnt is not { } petriDish)
                continue;

            if (!TryComp<CytologyPetriDishComponent>(petriDish, out var cytologyPetriDishComp) ||
                cytologyPetriDishComp == null)
                continue;

            ProcessGrowth(uid, petriDish, beakerSolution, cytologyPetriDishComp);

        }
    }

    private void OnGetVerb(Entity<CytologyGrowingVatComponent> growingVat, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract || args.Hands == null)
            return;

        AlternativeVerb verb = new()
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
        _appearance.SetData(growingVat.Owner, CytologyGrowingVatVisuals.Working, true);
    }

    private void ToggleOff(Entity<CytologyGrowingVatComponent> growingVat)
    {
        growingVat.Comp.IsActive = false;
        _appearance.SetData(growingVat.Owner, CytologyGrowingVatVisuals.Working, false);
    }

    private void OnPowerChanged(Entity<CytologyGrowingVatComponent> growingVat, ref PowerChangedEvent args)
    {
        _appearance.SetData(growingVat.Owner, CytologyGrowingVatVisuals.Powered, args.Powered);
    }

    public bool TryGetSolutionFromBeaker(EntityUid uid, out Solution solution)
    {
        solution = default!;

        var beakerEnt = _itemSlotsSystem.GetItemOrNull(uid, BeakerSlotName);
        if (beakerEnt is not { } beaker)
            return false;

        if (!_solutionContainerSystem.TryGetFitsInDispenser(beaker, out _, out var sol))
            return false;

        solution = sol;
        return true;
    }


    private bool TryGetCellSemplesFromPetriDish(EntityUid uid, out List<CellSample> cellSamples)
    {
        cellSamples = default!;

        var petriDishContainer = _itemSlotsSystem.GetItemOrNull(uid, PetriDishSlotName);

        if (petriDishContainer is not { } petriDish)
            return false;


        _solutionContainerSystem.TryGetSolution(uid, null, out _, out var sln1);
        if (TryComp<CytologyPetriDishComponent>(petriDishContainer, out var cytologyPetriDishComp) ||
            cytologyPetriDishComp?.CellSamples == null)
            return false;

        cellSamples = cytologyPetriDishComp.CellSamples;
        return true;
    }

    private void ProcessGrowth(EntityUid vat, EntityUid petriDish, Solution beakerSolution, CytologyPetriDishComponent cytologyPetriDishComp)
    {
        var cellSamples = cytologyPetriDishComp.CellSamples;
        if (cellSamples.Count == 0)
            return;

        var seconds = 1f;

        var reagentLookup = new Dictionary<string, FixedPoint2>();
        foreach (var rq in beakerSolution.Contents)
        {
            var id = rq.Reagent.Prototype;
            if (!reagentLookup.TryAdd(id, rq.Quantity))
                reagentLookup[id] += rq.Quantity;
        }

        for (var i = cellSamples.Count - 1; i >= 0; i--)
        {
            var cell = cellSamples[i];
            if (!_prototypeManager.TryIndex<CellSamplePrototype>(cell.ProtoID, out var proto))
                continue;

            var hasAllRequired = true;
            foreach (var required in proto.RequiredChemicals)
            {
                if (!reagentLookup.TryGetValue(required, out var qty) || qty <= FixedPoint2.Zero)
                {
                    hasAllRequired = false;
                    break;
                }
            }

            if (!hasAllRequired)
                continue;

            var modifier = 1f;
            foreach (var (chem, mult) in proto.SupplementaryChemicals)
            {
                if (reagentLookup.TryGetValue(chem, out var qty) && qty > FixedPoint2.Zero)
                    modifier += mult;
            }
            foreach (var (chem, mult) in proto.SuppressiveChemicals)
            {
                if (reagentLookup.TryGetValue(chem, out var qty) && qty > FixedPoint2.Zero)
                    modifier += mult;
            }
            if (modifier < 0.1f)
                modifier = 0.1f;

            var basePerSecond = 1f / MathF.Max(0.001f, proto.GrowthRateInSeconds);
            var delta = basePerSecond * seconds;
            cell.GrowProgress += delta * modifier;

            foreach (var required in proto.RequiredChemicals)
            {
                beakerSolution.RemoveReagent(required, FixedPoint2.New(1));
            }
            foreach (var (chem, _) in proto.SupplementaryChemicals)
            {
                beakerSolution.RemoveReagent(chem, FixedPoint2.New(1));
            }
            foreach (var (chem, _) in proto.SuppressiveChemicals)
            {
                beakerSolution.RemoveReagent(chem, FixedPoint2.New(1));
            }

            if (cell.GrowProgress >= 1f && proto.SpawnMobByPrototype != null)
            {
                foreach(var mob in proto.SpawnMobByPrototype)
                {
                    Spawn(mob, Transform(petriDish).Coordinates);
                }
                cellSamples.RemoveAt(i);
            }
        }
    }

    private void OnMapInit(EntityUid uid, CytologyGrowingVatComponent component, MapInitEvent args)
    {
        _appearance.SetData(uid, CytologyGrowingVatVisualLayers.Indicator, false);
        _appearance.SetData(uid, CytologyGrowingVatVisualLayers.Liquid, false);
    }

    private void WriteCellSamplesInGrowthProgress(CytologyPetriDishComponent CytologyPetriDishComp)
    {

    }
}
