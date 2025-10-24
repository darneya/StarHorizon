using Content.Shared._Horizon.Cytology.Components;
using Content.Shared._Horizon.Cytology.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared._Horizon.Cytology.Systems;

public abstract class SharedCytologySwabSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly CytologyDirtSystem _dirtSystem = default!;
    [Dependency] private readonly SharedPetriDishSystem _petriDishSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CytologySwabComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CytologySwabComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CytologySwabComponent, CytologySwabTakeDirtDoAfterEvent>(OnTakeDirtDoAfter);
        SubscribeLocalEvent<CytologySwabComponent, CytologySwabTransferToPetriDishDoAfterEvent>(OnTransferToPetriDishDoAfter);
    }

    private void OnExamined(EntityUid uid, CytologySwabComponent component, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            if (component.IsUsed && component.CellSamples.Count > 0)
                args.PushMarkup(Loc.GetString("cytology-swab-used", ("samples", component.CellSamples.Count)));
            else
                args.PushMarkup(Loc.GetString("cytology-swab-unused"));
        }
    }

    private void OnAfterInteract(EntityUid uid, CytologySwabComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach)
            return;

        TryCollectCellsFromDirt(uid, component, args);
        TryTransferCellsToPetriDish(uid, component, args);
    }

    private void OnTakeDirtDoAfter(Entity<CytologySwabComponent> swab, ref CytologySwabTakeDirtDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (!TryComp<CytologyDirtComponent>(args.Args.Target, out var dirtComp))
            return;

        if (!_dirtSystem.HasSamples(args.Args.Target.Value, dirtComp))
            return;

        var samples = _dirtSystem.GetCellSamples(args.Args.Target.Value, dirtComp);
        var samplesToCollect = samples; //TODO упростим

        swab.Comp.CellSamples.AddRange(samplesToCollect);
        swab.Comp.IsUsed = true;

        if (samples.Count > 0 && _prototypeManager.TryIndex<CellSamplePrototype>(samples.Last().ProtoID, out var proto))
        {
            swab.Comp.TextureState = proto.TextureState;
            Appearance.SetData(swab.Owner, CytologySwabVisualStates.IsVisible, true);
        }

        _dirtSystem.CleanDirt(args.Args.Target.Value, dirtComp);

        _popupSystem.PopupEntity(Loc.GetString("cytology-swab-collected", ("samples", samplesToCollect.Count)), args.Args.Target.Value, args.Args.User);
        args.Handled = true;
    }

    private void OnTransferToPetriDishDoAfter(Entity<CytologySwabComponent> swab, ref CytologySwabTransferToPetriDishDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (!TryComp<CytologyPetriDishComponent>(args.Args.Target, out var petriDishComp))
            return;

        if (petriDishComp.CellSamples.Count + args.CellSamples.Count > petriDishComp.MaxSamples)
            return;

        var cellSamples = args.CellSamples;

        petriDishComp.CellSamples.AddRange(cellSamples); // TODO это лишает уникальности
        swab.Comp.CellSamples.RemoveAll(x => cellSamples.Contains(x));

        if (swab.Comp.CellSamples.Count() <= 0)
        {
            Appearance.SetData(swab.Owner, CytologySwabVisualStates.IsVisible, false);
        }

        _petriDishSystem.PetriDishUpdateAppearance(args.Args.Target, petriDishComp);

        args.Handled = true;
    }

    public bool TryTransferSamples(EntityUid swabUid, EntityUid petriDishUid, CytologySwabComponent? swab = null, CytologyPetriDishComponent? petriDish = null)
    {
        if (!Resolve(swabUid, ref swab) || !Resolve(petriDishUid, ref petriDish))
            return false;

        if (!swab.IsUsed || swab.CellSamples.Count == 0)
            return false;

        if (petriDish.CellSamples.Count >= petriDish.MaxSamples)
            return false;

        var samplesToTransfer = swab.CellSamples;

        petriDish.CellSamples.AddRange(samplesToTransfer);
        petriDish.IsUsed = true;

        swab.CellSamples.Except(samplesToTransfer);
        if (swab.CellSamples.Count == 0)
            swab.IsUsed = false;

        return true;
    }

    private void TryCollectCellsFromDirt(EntityUid uid, CytologySwabComponent component, AfterInteractEvent args)
    {
        if (!TryComp<CytologyDirtComponent>(args.Target, out var dirt))
            return;

        if (!_dirtSystem.HasSamples(args.Target.Value, dirt)) // TODO упростим, а также вынесем в говорящие функции
        {
            _popupSystem.PopupEntity(Loc.GetString("cytology-swab-no-samples"), args.Target.Value, args.User);
            return;
        }

        if (component.IsUsed && component.CellSamples.Count >= component.MaxSamples)
        {
            _popupSystem.PopupEntity(Loc.GetString("cytology-swab-full"), uid, args.User);
            return;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.SwabDelay, new CytologySwabTakeDirtDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void TryTransferCellsToPetriDish(EntityUid uid, CytologySwabComponent component, AfterInteractEvent args)
    {
        if (!TryComp<CytologyPetriDishComponent>(args.Target, out var petriDishComp))
            return;

        List<CellSample> transferSamples = new List<CellSample>();

        foreach(var sample in component.CellSamples)
        {
            if(petriDishComp.CellSamples.Count == petriDishComp.MaxSamples)
            {
                _popupSystem.PopupEntity(Loc.GetString("cytology-petri-dish-is-full"), args.Target.Value, args.User);
                continue;
            }

            transferSamples.Add(sample);
        }

        if (transferSamples.Count <= 0)
            return;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.SwabDelay, new CytologySwabTransferToPetriDishDoAfterEvent(transferSamples), uid, target: args.Target, used: uid)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }
}
