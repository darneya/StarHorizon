using Content.Shared._Horizon.Cytology.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using System.Linq;

namespace Content.Shared._Horizon.Cytology.Systems;

public sealed class SharedCytologySwabSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly CytologyDirtSystem _dirtSystem = default!;

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

    private void OnTakeDirtDoAfter(EntityUid uid, CytologySwabComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (!TryComp<CytologyDirtComponent>(args.Args.Target, out var dirtComp))
            return;

        if (!_dirtSystem.HasSamples(args.Args.Target.Value, dirtComp))
            return;

        var samples = _dirtSystem.GetCellSamples(args.Args.Target.Value, dirtComp);
        var samplesToCollect = samples; //TODO упростим

        component.CellSamples.AddRange(samplesToCollect);
        component.IsUsed = true;

        _dirtSystem.CleanDirt(args.Args.Target.Value, dirtComp);

        _popupSystem.PopupEntity(Loc.GetString("cytology-swab-collected", ("samples", samplesToCollect.Count)), args.Args.Target.Value, args.Args.User);

        //Dirty(uid, component); // TODO надо будет вынести в дирт филд
        args.Handled = true;
    }

    private void OnTransferToPetriDishDoAfter(EntityUid uid, CytologySwabComponent component, CytologySwabTransferToPetriDishDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (!TryComp<CytologyPetriDishComponent>(args.Args.Target, out var petriDishComp))
            return;

        if (petriDishComp.CellSamples.Count + args.CellSamples.Count > petriDishComp.MaxSamples)
            return;

        petriDishComp.CellSamples.AddRange(args.CellSamples); // TODO это лишает уникальности

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

        //Dirty(swabUid, swab);
        //Dirty(petriDishUid, petriDish); //TODO Возможно, можно обойтись без этого

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
