using Content.Shared._Horizon.Cytology.Components;
using Content.Shared._Horizon.Cytology.Prototypes;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Timing;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Random;
using Content.Shared.Power;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared._Horizon.Cytology.Systems;

public abstract class SharedGrowingVatSystem : EntitySystem
{

    public const string BeakerSlotName = "beakerSlot";
    public const string PetriDishSlotName = "petriDishSlot";


    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CytologyGrowingVatComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CytologyGrowingVatComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CytologyGrowingVatComponent, EntInsertedIntoContainerMessage>(OnSolutionContainerChanged);
        SubscribeLocalEvent<CytologyGrowingVatComponent, EntRemovedFromContainerMessage>(OnSolutionContainerChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CytologyGrowingVatComponent>();
        while (query.MoveNext(out var uid, out var cytologyGrowingVatComp))
        {

            if (!cytologyGrowingVatComp.IsActive)
                continue;

            Entity<CytologyGrowingVatComponent> growingVat = (uid, cytologyGrowingVatComp);

            if (growingVat.Comp.NextUpdate > _timing.CurTime)
                continue;

            growingVat.Comp.NextUpdate = _timing.CurTime + growingVat.Comp.UpdateInterval;

            var dishEnt = _itemSlotsSystem.GetItemOrNull(uid, PetriDishSlotName);

            if (dishEnt is not { } petriDishUid)
                continue;

            ProcessGrowth(growingVat, petriDishUid);

        }
    }

    private void OnPowerChanged(Entity<CytologyGrowingVatComponent> growingVat, ref PowerChangedEvent args)
    {
        Appearance.SetData(growingVat.Owner, CytologyGrowingVatVisualStates.Powered, args.Powered);
    }

    public bool TryGetSolutionFromBeaker(EntityUid uid, [NotNullWhen(true)] out Solution? solution, [NotNullWhen(true)] out Entity<SolutionComponent> solutionEntity)
    {
        solutionEntity = default;
        solution = default;

        var beakerEnt = _itemSlotsSystem.GetItemOrNull(uid, BeakerSlotName);

        if (beakerEnt is not { } beakerEntUid)
            return false;

        if (!_solutionContainerSystem.TryGetFitsInDispenser(beakerEntUid, out var solutionComp, out solution))
            return false;

        solutionEntity = solutionComp.Value;

        return true;
    }

    private void OnSolutionContainerChanged<T>(Entity<CytologyGrowingVatComponent> growingVat, ref T ev)
    {
        Appearance.SetData(growingVat.Owner, CytologyGrowingVatVisualStates.WithLiquid, TryGetSolutionFromBeaker(growingVat.Owner, out _, out _));
    }

    private void ProcessGrowth(Entity<CytologyGrowingVatComponent> growingVat, EntityUid petriDish)
    {

        if (!TryComp<CytologySampleContainerComponent>(petriDish, out var petriDishSampleContainerComp))
            return;

        var beakerEnt = _itemSlotsSystem.GetItemOrNull(growingVat.Owner, BeakerSlotName);

        if (!TryGetSolutionFromBeaker(growingVat.Owner, out var beakerSolution, out var solutionEntity))
            return;

        Appearance.SetData(growingVat.Owner, CytologyGrowingVatVisualStates.IsError, growingVat.Comp.StopWithError);
        Appearance.SetData(growingVat.Owner, CytologyGrowingVatVisualStates.WithFoam, growingVat.Comp.WithFoam);
        growingVat.Comp.WithFoam = false;
        growingVat.Comp.StopWithError = true;

        var cellSamples = petriDishSampleContainerComp.CellSamples;
        if (cellSamples.Count == 0)
            return;

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

            SetGrowProgress(proto, cell, reagentLookup);

            ConsumeChemicals(solutionEntity, proto.RequiredChemicals);
            ConsumeChemicals(solutionEntity, proto.SupplementaryChemicals.Keys);
            ConsumeChemicals(solutionEntity, proto.SuppressiveChemicals.Keys);

            growingVat.Comp.StopWithError = false;
            growingVat.Comp.WithFoam = true;

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

    private void SetGrowProgress(CellSamplePrototype proto, CellSample cell, Dictionary<string, FixedPoint2> reagentLookup)
    {
        var modifier = 1f;

        modifier = ApplyModifiers(modifier, proto.SupplementaryChemicals, reagentLookup);
        modifier = ApplyModifiers(modifier, proto.SupplementaryChemicals, reagentLookup);

        if (modifier < 0.1f)
            modifier = 0.1f;

        var basePerSecond = 1f / MathF.Max(0.001f, proto.GrowthRateInSeconds);
        cell.GrowProgress += basePerSecond * modifier;
    }

    private float ApplyModifiers(float modifier, Dictionary<ProtoId<ReagentPrototype>, float> chemicals, Dictionary<string, FixedPoint2> reagentLookup)
    {
        foreach (var (chem, mult) in chemicals)
        {
            if (reagentLookup.TryGetValue(chem, out var qty) && qty > FixedPoint2.Zero)
                modifier += mult;
        }

        return modifier;
    }

    private void ConsumeChemicals(Entity<SolutionComponent> beakerEnt, IEnumerable<ProtoId<ReagentPrototype>> chemicals)
    {
        foreach (var chem in chemicals)
        {
            _solutionContainerSystem.RemoveReagent(beakerEnt, chem, FixedPoint2.New(1));
        }
    }

    private void OnMapInit(EntityUid uid, CytologyGrowingVatComponent component, MapInitEvent args)
    {
        Appearance.SetData(uid, CytologyGrowingVatVisualLayers.Indicator, false);
        Appearance.SetData(uid, CytologyGrowingVatVisualLayers.Liquid, false);
    }
}
