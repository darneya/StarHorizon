using System.Diagnostics.CodeAnalysis;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.OutpostCapture;

public sealed class OutpostCaptureSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = null!;
    [Dependency] private readonly IPrototypeManager _protoManager = null!;
    [Dependency] private readonly ILogManager _log = default!;

    private ISawmill _sawmill = null!;

    public override void Initialize()
    {
        SawmillInit();

        SubscribeLocalEvent<OutpostCaptureComponent, ComponentRemove>(OnCompRemove);

        SubscribeLocalEvent<OutpostConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<OutpostConsoleComponent, ComponentRemove>(OnConsoleRemove);

        SubscribeLocalEvent<OutpostConsoleComponent, OutpostChangeState>(OnMessage);
    }

    private void SawmillInit()
    {
        _sawmill = _log.GetSawmill("OutpostCapture");
        _sawmill.Info("_outpostCaptureSystem start his work!");
    }

    #region Component logic
    private void OnCompRemove(Entity<OutpostCaptureComponent> entity, ref ComponentRemove args)
    {
        if (entity.Comp.LinkedConsoles.Count == 0)
            return;

        UnlinkConsoles(entity.Comp.LinkedConsoles); // Отключаем консоли от аванпоста
    }

    private void OnConsoleInit(Entity<OutpostConsoleComponent> entity, ref ComponentInit args)
    {
        if (!TryGetOutpost(entity, out var outpost))
            return;

        outpost.LinkedConsoles.Add(entity);
    }

    private void OnConsoleRemove(Entity<OutpostConsoleComponent> entity, ref ComponentRemove args)
    {
        if (!TryGetOutpost(entity, out var outpost))
            return;

        outpost.LinkedConsoles.Remove(entity);
    }

    public bool TryGetOutpost(Entity<OutpostConsoleComponent> entity, [NotNullWhen(true)] out OutpostCaptureComponent? outpost)
    {
        outpost = null;
        var grid = _transform.GetGrid(Transform(entity).Coordinates);
        return grid != null && TryComp(grid, out outpost);
    }

    public bool UnlinkConsoles(List<Entity<OutpostConsoleComponent>> consoles)
    {
        var success = true;
        foreach (var (console, _) in consoles)
        {
            if (!Exists(console))
            {
                _sawmill.Error($"Консоль в списке подключенных к аванпосту, под uid: {console} была удалена, но не очищена из списка.");
                success = false;
                continue;
            }

            if (TryComp<OutpostConsoleComponent>(console, out var component))
            {
                component.LinkedOutpost = null;
                component.FactionCaptured = null;
            }
            else
            {
                _sawmill.Error($"Консоль под uid: '{console}' потеряла компонент OutpostConsole, может удалили во время работы?");
                success = false;
            }
        }

        return success;
    }
    #endregion

    #region UI messages
    private void OnMessage(Entity<OutpostConsoleComponent> entity, ref OutpostChangeState _)
    {
        var faction = new FactionComponent();
        if (faction is null)
            return;

        switch (entity.Comp.State)
        {
            case OutpostConsoleState.Capturing:
                _sawmill.Info($"Stop capturing console! at console {entity.Owner}");
                StopCapture(entity);
                break;

            case OutpostConsoleState.Uncaptured:
                _sawmill.Info($"Start capturing point! at console {entity.Owner}");
                StartCapture(entity, faction);
                break;

            case OutpostConsoleState.Captured:
                _sawmill.Info($"Uncapturing captured console! at console {entity.Owner}");
                StartCapture(entity, faction);
                break;

            default:
                _sawmill.Error("Unknown outpost capture state!");
                break;
        }
    }

    private void StopCapture(Entity<OutpostConsoleComponent> entity)
    {
        if (entity.Comp.LinkedOutpost == null
            || !TryComp<OutpostCaptureComponent>(entity.Comp.LinkedOutpost, out var outpost))
            return;

        entity.Comp.CapturingTime = null;
        entity.Comp.State = OutpostConsoleState.Uncaptured;
        outpost.CapturingConsoles.Remove(entity);
    }

    private void StartCapture(Entity<OutpostConsoleComponent> entity, FactionComponent faction)
    {
        if (entity.Comp.LinkedOutpost == null
            || !TryComp<OutpostCaptureComponent>(entity.Comp.LinkedOutpost, out var outpost))
            return;

        entity.Comp.FactionCaptured = faction.FactionName;
        entity.Comp.CapturingTime = entity.Comp.CaptureTime;
        entity.Comp.State = OutpostConsoleState.Capturing;
        outpost.CapturingConsoles.Add(entity);
    }

    private static void SetConsoleCaptured(Entity<OutpostConsoleComponent> entity, OutpostCaptureComponent outpost)
    {
        outpost.CapturedConsoles += 1;
        outpost.CapturingConsoles.Remove(entity);

        entity.Comp.CapturingTime = null;
        entity.Comp.State = OutpostConsoleState.Captured;
    }
    #endregion

    #region OutpostLogic

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        var inSeconds = TimeSpan.FromSeconds(deltaTime);
        while (EntityManager.AllEntityQueryEnumerator<OutpostCaptureComponent>().MoveNext(out var outpost))
        {
            if (outpost.ActualSpawnCooldown != null)
                outpost.ActualSpawnCooldown -= inSeconds;

            foreach (var entity in outpost.CapturingConsoles)
            {
                if (!Exists(entity))
                {
                    outpost.CapturingConsoles.Remove(entity);
                    outpost.LinkedConsoles.Remove(entity);
                    continue;
                }

                entity.Comp.CapturingTime -= inSeconds;
                if (entity.Comp.CapturingTime > TimeSpan.Zero)
                    continue;

                SetConsoleCaptured(entity, outpost);
            }

            if (outpost.CapturedConsoles < outpost.NeedCapturedConsoles
                || outpost.SpawningPointUid == null
                || outpost.SpawnList == null)
                continue;

            // ReSharper disable once InvertIf потому, что потому.
            if (outpost.ActualSpawnCooldown == null || outpost.ActualSpawnCooldown <= TimeSpan.Zero)
            {
                var coords = Transform(outpost.SpawningPointUid.Value).Coordinates;
                outpost.ActualSpawnCooldown = TrySpawnListItems(outpost.SpawnList.Value, coords, outpost.SpawnCooldown);
            }
        }
    }

    private TimeSpan? TrySpawnListItems(ProtoId<OutpostSpawnList> list, EntityCoordinates coords, TimeSpan cooldown)
    {
        var index = _protoManager.Index(list);
        var spawnList = index.SpawnList;
        foreach (var spawn in spawnList)
        {
            if (spawn.PrototypeId == null)
                continue;

            for (var i = spawn.GetAmount(); i != 0; i--)
            {
                SpawnAtPosition(spawn.PrototypeId.Value, coords);
            }
        }
        return cooldown;
    }
    #endregion
}
