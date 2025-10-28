using Content.Shared._Horizon.Cytology.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using System.Linq;


namespace Content.Shared._Horizon.Cytology.Systems;

public sealed class SharedInjectorSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly CytologyDirtSystem _dirtSystem = default!;
    [Dependency] private readonly SharedPetriDishSystem _petriDishSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CytologyInjectorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CytologyInjectorComponent, CytologyInjectorTakeDoAfterEvent>(OnTakeDoAfter);
    }

    private void OnAfterInteract(Entity<CytologyInjectorComponent> injector, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach)
            return;

        _petriDishSystem.TryTransferCellsToPetriDish(injector.Owner, args.Target, args.User);
        TryCollectCellsFromCreature(injector, args);

        if (!TryComp<CytologySampleContainerComponent>(injector.Owner, out var injectorSampleContainerComp))
            return;

        _appearance.SetData(injector.Owner, CytologyInjectorVisualStates.HasSamples, injectorSampleContainerComp.CellSamples.Count() > 0);
    }

    private void TryCollectCellsFromCreature(Entity<CytologyInjectorComponent> injector, AfterInteractEvent args)
    {
        if (!TryComp<SampleSourceComponent>(args.Target, out var sampleSourceComp))
            return;

        if (!TryComp<CytologySampleContainerComponent>(injector.Owner, out var injectorSampleContainerComp))
            return;

        if (sampleSourceComp.AvailableCellSamples == null)
            return;

        if (injectorSampleContainerComp.CellSamples.Count >= injectorSampleContainerComp.MaxSamples)
        {
            _popupSystem.PopupClient(Loc.GetString("cytology-injector-full"), injector.Owner, args.User);
            return;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, injector.Comp.TakeDelay, new CytologyInjectorTakeDoAfterEvent(), injector.Owner, target: args.Target, used: injector.Owner)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnTakeDoAfter(Entity<CytologyInjectorComponent> injector, ref CytologyInjectorTakeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (!TryComp<SampleSourceComponent>(args.Args.Target, out var sampleSourceComp))
            return;

        if (!TryComp<CytologySampleContainerComponent>(injector.Owner, out var injectorSampleContainerComp))
            return;

        //TODO дать комментарии и перенести в другие фунции
        var availableSpace = injectorSampleContainerComp.MaxSamples - injectorSampleContainerComp.CellSamples.Count();
        var collectedCells = sampleSourceComp.AvailableCellSamples.Take(availableSpace).ToList();

        injectorSampleContainerComp.CellSamples.AddRange(collectedCells);

        _popupSystem.PopupClient(Loc.GetString("cytology-injector-collected"), args.Args.Target.Value, args.Args.User);

        _appearance.SetData(injector.Owner, CytologyInjectorVisualStates.HasSamples, injectorSampleContainerComp.CellSamples.Count() > 0);

        args.Handled = true;
    }

}
