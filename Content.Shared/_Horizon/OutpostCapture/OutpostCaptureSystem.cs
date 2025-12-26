using System.Diagnostics.CodeAnalysis;
using Content.Shared._Horizon.FlavorText;
using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Horizon.OutpostCapture;

public sealed class OutpostCaptureSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = null!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = null!;

    [Dependency] private readonly SharedTransformSystem _transform = null!;
    [Dependency] private readonly ILogManager _logManager = null!;

    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = null!;
    private ISawmill _sawmill = null!;

    public override void Initialize()
    {
        SawmillInit();

        SubscribeLocalEvent<OutpostConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<OutpostConsoleComponent, ComponentRemove>(OnConsoleRemove);

        SubscribeLocalEvent<OutpostCaptureComponent, ComponentInit>(OnOutpostInit);
        SubscribeLocalEvent<OutpostCaptureComponent, ComponentRemove>(OnOutpostRemove);

        Subs.BuiEvents<OutpostConsoleComponent>(CaptureUIKey.Key,
        subs =>
        {
            subs.Event<OpenBoundInterfaceMessage>((uid, comp, _) => UpdateConsole((uid, comp)));
            subs.Event<OutpostCaptureUIStateCall>((uid, comp, _) => UpdateConsole((uid, comp)));
            subs.Event<OutpostCaptureButtonPressed>((uid, comp, _) => OnButtonPressed((uid, comp)));
        });
    }

    #region Intialize
    private void SawmillInit()
    {
        _sawmill = _logManager.GetSawmill("OutpostCapture");
        _sawmill.Info("_outpostCaptureSystem start his work!");
    }

    private void OnConsoleInit(Entity<OutpostConsoleComponent> console, ref ComponentInit args)
    {
        if (TryGetOutpost(console, out var outpost, out var outpostUid))
            return;

        if (console.Comp.CanUseAsSpawnPoint &&
            _transform.TryGetMapOrGridCoordinates(console, out var coords))
            outpost.SpawnLocation = coords;

        console.Comp.LinkedOutpost = GetNetEntity(outpostUid);
        outpost.LinkedConsoles.Add(GetNetEntity(console));
        _itemSlotsSystem.AddItemSlot(console, console.Comp.ContainerSlot, console.Comp.IdCardSlot);
    }

    private void OnConsoleRemove(Entity<OutpostConsoleComponent> console, ref ComponentRemove args)
    {
        if (TryGetOutpost(console, out var outpost, out _))
            return;

        console.Comp.LinkedOutpost = null;
        outpost.LinkedConsoles.Remove(GetNetEntity(console));
        outpost.CapturedConsoles.Remove(GetNetEntity(console));
        _itemSlotsSystem.RemoveItemSlot(console, console.Comp.IdCardSlot);
    }

    private void OnOutpostInit(Entity<OutpostCaptureComponent> outpost, ref ComponentInit args)
    {
        if (outpost.Comp.NeedCaptured <= 0)
        {
            RemComp<OutpostCaptureComponent>(outpost);
            return;
        }

        outpost.Comp.NextSpawn = outpost.Comp.SpawnCooldown;
    }

    private void OnOutpostRemove(Entity<OutpostCaptureComponent> outpost, ref ComponentRemove args)
    {
        if (outpost.Comp.LinkedConsoles.Count <= 0 ||
            outpost.Comp.CapturedConsoles.Count <= 0)
            return;

        UnlinkAllConsoles(outpost.Comp);
    }

    private bool TryGetOutpost(EntityUid uid,
        [NotNullWhen(false)] out OutpostCaptureComponent? outpost,
        [NotNullWhen(false)] out EntityUid? grid)
    {
        outpost = null;
        grid = _transform.GetGrid(Transform(uid).Coordinates);
        return grid == null || !TryComp(grid, out outpost);
    }
    #endregion

    #region Public Methods
    public void UnlinkAllConsoles(OutpostCaptureComponent outpost)
    {
        foreach (var console in outpost.LinkedConsoles)
        {
            if (TryGetEntity(console, out var actualConsole) ||
                !TryComp<OutpostConsoleComponent>(actualConsole, out var consoleComp))
                continue;

            consoleComp.LinkedOutpost = null;
            consoleComp.CapturedFaction = null;
            consoleComp.CapturingTime = null;
        }
    }

    public void UnlinkAllConsoles(EntityUid grid)
    {
        if (!TryComp<OutpostCaptureComponent>(grid, out var outpost))
            return;

        UnlinkAllConsoles(outpost);
    }

    public CharacterFactionPrototype? TryGetFactionInSlot(Entity<OutpostConsoleComponent> console)
    {
        var slot = _containerSystem.EnsureContainer<ContainerSlot>(console,
            console.Comp.ContainerSlot,
            out var existed);

        if (!existed || slot.ContainedEntity == null)
            return null;

        var id = slot.ContainedEntity.Value;
        if (!TryComp<IdCardComponent>(id, out var card) || card.JobPrototype == null)
            return null;

        var prototype = card.JobPrototype.Value;
        if (!_prototypeManager.TryIndex(prototype, out var index))
            return null;

        var faction = index.ForceFaction;
        return !_prototypeManager.TryIndex(faction, out var factionIndex) ? null : factionIndex;
    }

    public bool CanChangeCaptureState(Entity<OutpostConsoleComponent> console,
        [NotNullWhen(true)] out CharacterFactionPrototype? faction)
    {
        faction = TryGetFactionInSlot(console);
        if (faction == null)
        {
            _sawmill.Info("Capturing attempt failed, cause: Cant resolve faction!");
            return false;
        }

        if (console.Comp.LinkedOutpost == null)
        {
            _sawmill.Info("Capturing attempt failed, cause: Cant resolve outpost!");
            return false;
        }

        if (console.Comp.CapturedFaction != faction.ID)
            return true;

        _sawmill.Info("Capturing attempt failed, cause: Dont give chance one faction capture outpost twice!");
        return false;
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        var secondPast = TimeSpan.FromSeconds(deltaTime);
        var enumerable = EntityManager.EntityQueryEnumerator<OutpostCaptureComponent>();
        while (enumerable.MoveNext(out var outpost))
        {
            if (outpost.SpawnLocation == null || !_prototypeManager.TryIndex(outpost.SpawnList, out var index))
                continue;

            var updatedCapturedConsoles = new List<NetEntity>();
            var updatedCapturingConsoles = new List<NetEntity>();
            foreach (var console in outpost.CapturingConsoles)
            {
                if (!TryGetEntity(console, out var actualConsole)
                    || !TryComp<OutpostConsoleComponent>(actualConsole, out var consoleComp))
                    continue;

                consoleComp.CapturingTime -= secondPast;
                if (_uiSystem.HasUi(actualConsole.Value, CaptureUIKey.Key))
                {
                    var message = new ProgressBarUpdate(UpdateProgressBar((actualConsole.Value, consoleComp)));
                    _uiSystem.ServerSendUiMessage(actualConsole.Value, CaptureUIKey.Key, message);
                }

                if (consoleComp.CapturingTime > TimeSpan.Zero)
                {
                    updatedCapturingConsoles.Add(console); // Not captured yet.
                    continue;
                }

                consoleComp.CapturingTime = null;
                updatedCapturedConsoles.Add(console); // Already captured.
            }

            outpost.CapturedConsoles = updatedCapturedConsoles;
            outpost.CapturingConsoles = updatedCapturingConsoles;

            if (outpost.CapturedConsoles.Count < outpost.NeedCaptured)
            {
                outpost.NextSpawn = outpost.SpawnCooldown;
                outpost.CapturedFaction = null;
                continue;
            }

            if (!TrySetCapturedFaction(outpost))
                continue;

            outpost.NextSpawn -= secondPast;
            if (outpost.NextSpawn > TimeSpan.Zero)
                continue;

            TrySpawnItemsInList(index, outpost.SpawnLocation.Value);
        }
    }

    public float? UpdateProgressBar(Entity<OutpostConsoleComponent> console)
    {
        var progress = (float?) (1f - console.Comp.CapturingTime / console.Comp.CaptureTime) * 100f;
        progress = progress != null ? float.Clamp(progress.Value, 0f, 100f) : null;
        return progress;
    }
    #endregion

    #region Private Methods
    private void ChangeCaptureState(Entity<OutpostConsoleComponent> console)
    {
        if (!CanChangeCaptureState(console, out var faction))
            return;

        var oldFaction = console.Comp.CapturedFaction;
        console.Comp.CapturedFaction = faction.ID;
        switch (console.Comp.State)
        {
            case OutpostConsoleState.Uncaptured:
                _sawmill.Info($"Faction {faction}, start capturing console id - {console.Owner}");
                break;

            case OutpostConsoleState.Capturing:
                _sawmill.Info($"Faction {faction}, intercepts capturing console faction, {oldFaction} on id - {console.Owner}");
                return;

            case OutpostConsoleState.Captured:
                _sawmill.Info($"Faction {faction}, start recapturing console of faction, {oldFaction} on id - {console.Owner}");
                break;

            default:
                _sawmill.Error("Don't know how to change default capture state!");
                return;
        }

        console.Comp.State = OutpostConsoleState.Capturing;
        StartCapture(GetNetEntity(console), console.Comp);
    }

    private void StartCapture(NetEntity netConsole, OutpostConsoleComponent console)
    {
        if (!TryGetEntity(console.LinkedOutpost, out var outpost) ||
            !TryComp<OutpostCaptureComponent>(outpost, out var outpostCapture))
            return;

        console.CapturingTime = console.CaptureTime;
        if (outpostCapture.CapturedConsoles.Contains(netConsole))
            outpostCapture.CapturedConsoles.Remove(netConsole);

        outpostCapture.CapturingConsoles.Add(netConsole);
    }

    private bool TrySetCapturedFaction(OutpostCaptureComponent outpost)
    {
        var factionList = new List<string>();
        foreach (var console in outpost.CapturedConsoles)
        {
            if (!TryGetEntity(console, out var actualConsole)
                || !TryComp<OutpostConsoleComponent>(actualConsole, out var consoleComp))
                continue;

            if (consoleComp.CapturedFaction == null)
                continue;

            if (factionList.Contains(consoleComp.CapturedFaction))
                continue;

            factionList.Add(consoleComp.CapturedFaction);
        }

        if (factionList.Count is > 1 or 0)
            return false;

        if (!_prototypeManager.TryIndex<CharacterFactionPrototype>(factionList[0], out var spawn))
            return false;

        if (spawn.OutpostSpawnListProto == null)
        {
            _sawmill.Error($"{spawn?.ID} имеет пустой список для спавна, после захвата. Стоит проверить правильность прототипа.");
            return false;
        }


        outpost.CapturedFaction = spawn.ID;
        outpost.SpawnList = spawn.OutpostSpawnListProto.Value;
        return true;
    }

    private void TrySpawnItemsInList(OutpostSpawnList list, EntityCoordinates location)
    {
        try
        {
            foreach (var entity in list.SpawnList)
            {
                SpawnAtPosition(entity.PrototypeId, location);
            }
        }
        catch (Exception e)
        {
            _sawmill.Error(e.Message);
        }
    }
    #endregion

    #region Messages
    private void UpdateConsole(Entity<OutpostConsoleComponent> console)
    {
        var progress = UpdateProgressBar(console);
        var buttonStateDisabled = !CanChangeCaptureState(console, out _);

        var state = new OutpostUIState(progress, buttonStateDisabled);
        _uiSystem.SetUiState(console.Owner, CaptureUIKey.Key, state);
    }

    private void OnButtonPressed(Entity<OutpostConsoleComponent> console)
    {
        ChangeCaptureState(console);
        UpdateConsole(console); // Update console
    }
    #endregion
}

[Serializable, NetSerializable]
public sealed class OutpostUIState(float? progress, bool disabled) : BoundUserInterfaceState
{
    public float? Progress => progress;
    public bool Disabled => disabled;
}
