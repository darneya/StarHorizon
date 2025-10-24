using Content.Shared._Horizon.Cytology.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared._Horizon.Cytology.Prototypes;
using Robust.Shared.Prototypes;
using Content.Shared.Verbs;

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
        if (args.IsInDetailsRange)
        {
            if (dish.IsUsed && dish.CellSamples.Count > 0)
                args.PushMarkup(Loc.GetString("cytology-dish-used", ("samples", dish.CellSamples.Count)));
            else
                args.PushMarkup(Loc.GetString("cytology-dish-unused"));
        }
    }

    public void ClearSamples(Entity<CytologyPetriDishComponent> petriDish)
    {
        petriDish.Comp.CellSamples.Clear();
        petriDish.Comp.IsUsed = false;
        PetriDishUpdateAppearance(petriDish.Owner, petriDish.Comp);
    }

    public List<CellSample> GetCellSamples(EntityUid uid, CytologyPetriDishComponent? dish = null)
    {
        if (!Resolve(uid, ref dish))
            return new List<CellSample>();

        return dish.CellSamples;
    }

    public void PetriDishUpdateAppearance(EntityUid? uid, CytologyPetriDishComponent dish)
    {
        if (uid is not { } petriDishUid)
            return;

        if(dish.CellSamples.Count > 0)
        {
            Appearance.SetData(petriDishUid, CytologyPetriDishVisualStates.HasSamples, true);
            Appearance.SetData(petriDishUid, CytologyPetriDishVisualStates.Color, CalculateAverageCellSampleColor(dish.CellSamples));
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
}
