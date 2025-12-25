using System.Diagnostics.CodeAnalysis;
using Content.Shared._Horizon.FlavorText;
using Content.Shared.Access.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Horizon.OutpostCapture;

public sealed class OutpostCaptureSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = null!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = null!;

    [Dependency] private readonly SharedTransformSystem _transform = null!;
    [Dependency] private readonly ILogManager _logManager = null!;
    private ISawmill _sawmill = null!;

    public override void Initialize()
    {
        SawmillInit();

        SubscribeLocalEvent<OutpostConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<OutpostConsoleComponent, ComponentRemove>(OnConsoleRemove);

        SubscribeLocalEvent<OutpostCaptureComponent, ComponentInit>(OnOutpostInit);
        SubscribeLocalEvent<OutpostCaptureComponent, ComponentRemove>(OnOutpostRemove);
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
    }

    private void OnConsoleRemove(Entity<OutpostConsoleComponent> console, ref ComponentRemove args)
    {
        if (TryGetOutpost(console, out var outpost, out _))
            return;

        console.Comp.LinkedOutpost = null;
        outpost.LinkedConsoles.Remove(GetNetEntity(console));
        outpost.CapturedConsoles.Remove(GetNetEntity(console));
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
        if (_prototypeManager.TryIndex(prototype, out var index))
            return null;

        var faction = index?.ForceFaction;
        return _prototypeManager.TryIndex(faction, out var factionIndex) ? null : factionIndex;
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

            foreach (var console in outpost.CapturingConsoles)
            {
                if (!TryGetEntity(console, out var actualConsole)
                    || !TryComp<OutpostConsoleComponent>(actualConsole, out var consoleComp))
                    continue;

                consoleComp.CapturingTime -= secondPast;
                if (consoleComp.CapturingTime > TimeSpan.Zero)
                    continue;

                consoleComp.CapturingTime = null;
                outpost.CapturedConsoles.Add(console);
                outpost.CapturingConsoles.Remove(console);
            }

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
    #endregion

    #region Private Methods
    private void ChangeCaptureState(Entity<OutpostConsoleComponent> console)
    {
        if (CanChangeCaptureState(console, out var faction) || faction == null)
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

        if (factionList.Count is > 1 or 0 || !factionList.TryFirstOrDefault(out var faction))
            return false;

        if (!_prototypeManager.TryIndex<CharacterFactionPrototype>(faction, out var spawn))
            return false;

        outpost.CapturedFaction = spawn.ID;
        outpost.SpawnList = spawn.OutpostSpawnListProto;
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
}
