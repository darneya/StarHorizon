using Content.Shared._Horizon.Cytology.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Shared._Horizon.Cytology.Systems;

public sealed class SharedPetriDishSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedCytologySwabSystem _swabSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CytologyPetriDishComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CytologyPetriDishComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CytologyPetriDishComponent, CytologyTransferDoAfterEvent>(OnDoAfter);
    }

    private void OnExamined(EntityUid uid, CytologyPetriDishComponent dish, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            if (dish.IsUsed && dish.CellSamples.Count > 0)
                args.PushMarkup(Loc.GetString("cytology-dish-used", ("samples", dish.CellSamples.Count)));
            else
                args.PushMarkup(Loc.GetString("cytology-dish-unused"));
        }
    }

    private void OnAfterInteract(EntityUid uid, CytologyPetriDishComponent dish, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach)
            return;

        if (!TryComp<CytologySwabComponent>(args.Target, out var swab))
            return;

        if (!swab.IsUsed || swab.CellSamples.Count == 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("cytology-dish-no-samples"), args.Target.Value, args.User);
            return;
        }

        if (dish.CellSamples.Count >= dish.MaxSamples)
        {
            _popupSystem.PopupEntity(Loc.GetString("cytology-dish-full"), uid, args.User);
            return;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, 1f, new CytologyTransferDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnDoAfter(EntityUid uid, CytologyPetriDishComponent dish, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (!TryComp<CytologySwabComponent>(args.Args.Target, out var swab))
            return;

        if (_swabSystem.TryTransferSamples(args.Args.Target.Value, uid, swab, dish))
        {
            _popupSystem.PopupEntity(Loc.GetString("cytology-dish-transferred"), uid, args.Args.User);
        }
        else
        {
            _popupSystem.PopupEntity(Loc.GetString("cytology-dish-transfer-failed"), uid, args.Args.User);
        }

        args.Handled = true;
    }

    public void ClearSamples(EntityUid uid, CytologyPetriDishComponent? dish = null)
    {
        if (!Resolve(uid, ref dish))
            return;

        dish.CellSamples.Clear();
        dish.IsUsed = false;
        //Dirty(uid, dish);
    }

    public List<CellSample> GetCellSamples(EntityUid uid, CytologyPetriDishComponent? dish = null)
    {
        if (!Resolve(uid, ref dish))
            return new List<CellSample>();

        return dish.CellSamples;
    }
}
