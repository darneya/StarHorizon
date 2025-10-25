using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Horizon.Cytology.Components;
using Content.Shared._Horizon.Cytology.Prototypes;
using Content.Shared._Horizon.Cytology.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Labels.EntitySystems;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Server._Horizon.Cytology.Components;


namespace Content.Server._Horizon.Cytology;

public sealed class MicroscopeSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly StorageSystem _storageSystem = default!;
    [Dependency] private readonly LabelSystem _labelSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CytologyMicroscopeComponent, ComponentStartup>(SubscribeUpdateUiState);
        SubscribeLocalEvent<CytologyMicroscopeComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
        SubscribeLocalEvent<CytologyMicroscopeComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
        SubscribeLocalEvent<CytologyMicroscopeComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
        SubscribeLocalEvent<CytologyMicroscopeComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);
    }

    private void SubscribeUpdateUiState<T>(Entity<CytologyMicroscopeComponent> ent, ref T ev)
    {
        UpdateUiState(ent);
    }

    private void UpdateUiState(Entity<CytologyMicroscopeComponent> ent)
    {
        var inputContainer = _itemSlotsSystem.GetItemOrNull(ent.Owner, SharedMicroscope.InputSlotName);

        var state = new MicroscopeBoundUserInterfaceState(BuildInputContainerInfo(inputContainer));

        _userInterfaceSystem.SetUiState(ent.Owner, MicroscopeUiKey.Key, state);
    }

    private List<CellSampleInfo>? BuildInputContainerInfo(EntityUid? container)
    {
        if (container is not { Valid: true })
            return null;

        if (!TryComp<CytologySampleContainerComponent>(container, out var petriDishSampleContainerComp))
            return null;

        List<CellSampleInfo> cellSampleInfos = new();

        foreach (var cellSample in petriDishSampleContainerComp.CellSamples)
        {
            if (!_prototypeManager.TryIndex<CellSamplePrototype>(cellSample.ProtoID, out var cellSamplePrototype))
                continue;

            cellSampleInfos.Add(BuildPetriDishInfo(cellSamplePrototype));
        }

        return cellSampleInfos;
    }

    private static CellSampleInfo BuildPetriDishInfo(CellSamplePrototype cellSamplePrototype)
    {
        return new CellSampleInfo(cellSamplePrototype.Name, cellSamplePrototype.RequiredChemicals, cellSamplePrototype.SupplementaryChemicals.Keys.ToList(),
                                  cellSamplePrototype.SuppressiveChemicals.Keys.ToList(), cellSamplePrototype.GrowthRateInSeconds, cellSamplePrototype.ViralSusceptibility);
    }
}
