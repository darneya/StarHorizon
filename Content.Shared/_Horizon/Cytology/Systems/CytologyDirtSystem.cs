using Content.Shared._Horizon.Cytology.Components;
using Robust.Shared.Random;
using System.Linq;
using Content.Shared.Examine;

namespace Content.Shared._Horizon.Cytology.Systems;

public sealed class CytologyDirtSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CytologyDirtComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CytologyDirtComponent, MapInitEvent>(OnMapInit);
    }

    private void OnExamined(EntityUid uid, CytologyDirtComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if(component.CurrentCellSamples.Count > 0)
            args.PushMarkup(Loc.GetString("cytology-polluted"));
    }
    private void OnMapInit(EntityUid uid, CytologyDirtComponent component, MapInitEvent args)
    {
        if (component.PossibleCellSamples.Count == 0)
            return;

        if (!_random.Prob(component.SampleChance)) // TODO может быть, пересмотреть логику
            return;

        var numSamples = _random.Next(1, component.PossibleCellSamples.Count);
        component.CurrentCellSamples = _random.GetItems(component.PossibleCellSamples, numSamples, false).ToList(); //TODO подумать над этим

        Dirty(uid, component);
    }

    public void CleanDirt(EntityUid uid, CytologyDirtComponent? component = null) //TODO не забыть при очистке уборщика удалять. Тоже, может
    {
        if (!Resolve(uid, ref component))
            return;

        component.CurrentCellSamples.Clear();
    }

    public bool HasSamples(EntityUid uid, CytologyDirtComponent? component = null) // TODO, избавиться МБ
    {
        if (!Resolve(uid, ref component))
            return false;

        return component.CurrentCellSamples.Count > 0;
    }
}
