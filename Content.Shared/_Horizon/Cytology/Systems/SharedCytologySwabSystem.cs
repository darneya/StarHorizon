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
    }

    private void OnExamined(EntityUid uid, CytologySwabComponent component, ExaminedEvent args)
    {
        if (!TryComp<CytologySampleContainerComponent>(uid, out var swabSampleContainerComp))
            return;

        if (args.IsInDetailsRange)
        {
            if (swabSampleContainerComp.CellSamples.Count > 0)
                args.PushMarkup(Loc.GetString("cytology-swab-used", ("samples", swabSampleContainerComp.CellSamples.Count)));
            else
                args.PushMarkup(Loc.GetString("cytology-swab-unused"));
        }
    }
    private void OnAfterInteract(EntityUid uid, CytologySwabComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach)
            return;

        TryCollectCellsFromDirt(uid, component, args);

        _petriDishSystem.TryTransferCellsToPetriDish(uid, args.Target, args.User);
        if(TryComp<CytologySampleContainerComponent>(uid, out var swabSampleContainerComp))
        {
            if(swabSampleContainerComp.CellSamples.Count() <= 0)
                Appearance.SetData(uid, CytologySwabVisualStates.IsVisible, false);
        }
    }

    private void OnTakeDirtDoAfter(Entity<CytologySwabComponent> swab, ref CytologySwabTakeDirtDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (!TryComp<CytologyDirtComponent>(args.Args.Target, out var dirtComp))
            return;

        if (!TryComp<CytologySampleContainerComponent>(swab.Owner, out var swabSampleContainerComp))
            return;

        if (!_dirtSystem.HasSamples(args.Args.Target.Value, dirtComp))
            return;

        var samples = _dirtSystem.GetCellSamples(args.Args.Target.Value, dirtComp);
        var samplesToCollect = samples; //TODO упростим

        swabSampleContainerComp.CellSamples.AddRange(samplesToCollect);

        if (samples.Count > 0 && _prototypeManager.TryIndex<CellSamplePrototype>(samples.Last().ProtoID, out var proto))
        {
            swab.Comp.TextureState = proto.TextureState;
            Appearance.SetData(swab.Owner, CytologySwabVisualStates.IsVisible, true);
        }

        _dirtSystem.CleanDirt(args.Args.Target.Value, dirtComp);

        _popupSystem.PopupEntity(Loc.GetString("cytology-swab-collected", ("samples", samplesToCollect.Count)), args.Args.Target.Value, args.Args.User);
        args.Handled = true;
    }

    private void TryCollectCellsFromDirt(EntityUid uid, CytologySwabComponent component, AfterInteractEvent args)
    {
        if (!TryComp<CytologyDirtComponent>(args.Target, out var dirt))
            return;

        if (!TryComp<CytologySampleContainerComponent>(uid, out var swabSampleContainerComp))
            return;

        if (!_dirtSystem.HasSamples(args.Target.Value, dirt)) // TODO упростим, а также вынесем в говорящие функции
        {
            _popupSystem.PopupEntity(Loc.GetString("cytology-swab-no-samples"), args.Target.Value, args.User);
            return;
        }

        if (swabSampleContainerComp.CellSamples.Count >= swabSampleContainerComp.MaxSamples)
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
}
