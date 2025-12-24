using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Map.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.OutpostCapture;

public sealed class OutpostCaptureSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = null!;
    [Dependency] private readonly ILogManager _log = default!;

    private ISawmill _sawmill = null!;

    public override void Initialize()
    {
        SawmillInit();

        SubscribeLocalEvent<OutpostCaptureComponent, ComponentRemove>(OnGridRemove);

        SubscribeLocalEvent<OutpostConsoleComponent, ComponentInit>(OnConsoleInit);
        SubscribeLocalEvent<OutpostConsoleComponent, ComponentRemove>(OnConsoleRemove);

        SubscribeLocalEvent<OutpostConsoleComponent, OutpostCaptureStartMessage>(OnMessage);
    }

    private void SawmillInit()
    {
        _sawmill = _log.GetSawmill("OutpostCapture");
        _sawmill.Info("_outpostCaptureSystem start his work!");
    }

    #region Component logic
    private void OnGridRemove(Entity<OutpostCaptureComponent> entity, ref ComponentRemove args)
    {
        if (entity.Comp.LinkedConsoles.Count == 0)
            return;

        UnlinkConsoles(entity.Comp.LinkedConsoles); // Отключаем консоли от аванпоста
    }

    private void OnConsoleInit(Entity<OutpostConsoleComponent> entity, ref ComponentInit args)
    {
        if (!TryGetOutpost(entity, out var outpost))
            return;

        LinkConsole(entity, outpost);
    }

    private void OnConsoleRemove(Entity<OutpostConsoleComponent> entity, ref ComponentRemove args)
    {
        if (!TryGetOutpost(entity, out var outpost))
            return;

        UnlinkConsole(entity, outpost);
    }
    #endregion

    #region Get/Add/Remove methods
    public bool TryGetOutpost(Entity<OutpostConsoleComponent> entity, [NotNullWhen(true)] out OutpostCaptureComponent? outpost)
    {
        outpost = null;
        var grid = _transform.GetGrid(Transform(entity).Coordinates);
        return grid != null && TryComp(grid, out outpost);
    }

    public bool UnlinkConsoles(HashSet<EntityUid> consoles)
    {
        var success = true;
        foreach (var console in consoles)
        {
            if (!Exists(console))
            {
                _sawmill.Error($"Консоль в списке объединённых к аванпосту под uid: {console} удалена.");
                success = false;
                continue;
            }

            if (TryComp<OutpostConsoleComponent>(console, out var component))
            {
                component.LinkedOutpost = null;
                component.FactionCaptured?.Clear();
            }
            else
            {
                _sawmill.Error($"Консоль под uid: '{console}' потеряла компонент OutpostConsole, может удалили во время работы?");
                success = false;
            }
        }

        return success;
    }

    public bool LinkConsole(EntityUid console, OutpostCaptureComponent outpost)
    {
        if (!HasComp<OutpostConsoleComponent>(console))
            return false;

        outpost.LinkedConsoles.Add(console);
        return true;
    }

    public bool UnlinkConsole(EntityUid console, OutpostCaptureComponent outpost)
    {
        if (outpost.LinkedConsoles.Contains(console))
            outpost.LinkedConsoles.Remove(console);
        else
            return false;

        return true;
    }
    #endregion

    #region UI messages
    private void OnMessage(Entity<OutpostConsoleComponent> entity, ref OutpostCaptureStartMessage message)
    {
        switch (message.State)
        {
            case OutpostStateEnum.Capturing:
                _sawmill.Info($"Stop capturing point! at console {entity.Owner}");
                break;
        }
    }
    #endregion

    [Serializable, NetSerializable]
    public sealed class OutpostCaptureStartMessage(OutpostStateEnum state) : BoundUserInterfaceMessage
    {
        public OutpostStateEnum State => state;
    }

    [Serializable, NetSerializable]
    public enum OutpostStateEnum : byte
    {
        Capturing,
        Released,
        Captured,
    }
}
