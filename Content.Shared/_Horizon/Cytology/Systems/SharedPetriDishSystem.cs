using Content.Shared._Horizon.Cytology.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared._Horizon.Cytology.Prototypes;
using Robust.Shared.Prototypes;
using Content.Shared.Verbs;
using System.Linq;

namespace Content.Shared._Horizon.Cytology.Systems;

public abstract class SharedPetriDishSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedCytologySwabSystem _swabSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CytologyPetriDishComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CytologyPetriDishComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
    }

    private void OnGetVerb(Entity<CytologyPetriDishComponent> petriDish, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract || args.Hands == null)
            return;

        AlternativeVerb verb = new()
        {
            Act = () => ClearSamples(petriDish),
            Text = Loc.GetString("verb-split-samples")
        };

        args.Verbs.Add(verb);

    }

    private void OnExamined(EntityUid uid, CytologyPetriDishComponent dish, ExaminedEvent args)
    {
        if (!TryComp<CytologySampleContainerComponent>(uid, out var petriDishSampleContainerComp))
            return;

        if (args.IsInDetailsRange)
        {
            if (dish.IsUsed && petriDishSampleContainerComp.CellSamples.Count > 0)
                args.PushMarkup(Loc.GetString("cytology-dish-used", ("samples", petriDishSampleContainerComp.CellSamples.Count)));
            else
                args.PushMarkup(Loc.GetString("cytology-dish-unused"));
        }
    }

    public void ClearSamples(Entity<CytologyPetriDishComponent> petriDish)
    {
        if (!TryComp<CytologySampleContainerComponent>(petriDish.Owner, out var petriDishSampleContainerComp))
            return;

        petriDishSampleContainerComp.CellSamples.Clear();
        petriDish.Comp.IsUsed = false;
        PetriDishUpdateAppearance(petriDish.Owner);
    }

    public List<CellSample> GetCellSamples(EntityUid uid)
    {
        if (!TryComp<CytologySampleContainerComponent>(uid, out var petriDishSampleContainerComp))
            return new List<CellSample>();

        return petriDishSampleContainerComp.CellSamples;
    }

    public void PetriDishUpdateAppearance(EntityUid? uid)
    {
        if (uid is not { } petriDishUid)
            return;

        if (!TryComp<CytologySampleContainerComponent>(uid, out var petriDishSampleContainerComp))
            return;

        if (petriDishSampleContainerComp.CellSamples.Count > 0)
        {
            Appearance.SetData(petriDishUid, CytologyPetriDishVisualStates.HasSamples, true);
            Appearance.SetData(petriDishUid, CytologyPetriDishVisualStates.Color, CalculateAverageCellSampleColor(petriDishSampleContainerComp.CellSamples));
        }
        else Appearance.SetData(petriDishUid, CytologyPetriDishVisualStates.HasSamples, false);

    }

    private Color CalculateAverageCellSampleColor(List<CellSample> cellSamples)
    {
        if (cellSamples.Count == 0)
            return Color.White;

        var colorSum = Vector3.Zero;
        var totalSamples = cellSamples.Count;

        foreach (var sample in cellSamples)
        {
            if (!_prototypeManager.TryIndex<CellSamplePrototype>(sample.ProtoID, out var proto))
                continue;

            var sampleColor = GetColorFromTextureState(proto.TextureState);
            var colorVector = new Vector3(sampleColor.R, sampleColor.G, sampleColor.B);
            colorSum += colorVector;
        }

        if (totalSamples == 0)
            return Color.White;

        var averageColorVector = colorSum / totalSamples;
        return new Color(
            Math.Clamp(averageColorVector.X, 0f, 1f),
            Math.Clamp(averageColorVector.Y, 0f, 1f),
            Math.Clamp(averageColorVector.Z, 0f, 1f),
            1f
        );
    }

    private Color GetColorFromTextureState(string? textureState)
    {
        return textureState switch
        {
            "black" => Color.Black,
            "yellow" => Color.Yellow,
            "green" => Color.Green,
            "brown" => Color.Brown,
            "violet" => Color.Purple,
            _ => Color.White
        };
    }

    public bool TryTransferCellsToPetriDish(EntityUid transferDevice, EntityUid? petriDish, EntityUid user)
    {

        if (!TryComp<CytologySampleContainerComponent>(transferDevice, out var transferDeviceSampleContainerComp) ||
            !TryComp<CytologySampleContainerComponent>(petriDish, out var petriDishSampleContainerComp))
            return false;

        var availableSpace = petriDishSampleContainerComp.MaxSamples - petriDishSampleContainerComp.CellSamples.Count();
        if(availableSpace <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("cytology-petri-dish-is-full"), petriDish.Value, user);
            return false;
        }
        var collectedCells = transferDeviceSampleContainerComp.CellSamples.Take(availableSpace).ToList();

        petriDishSampleContainerComp.CellSamples.AddRange(collectedCells);

        return true;
    }
}
