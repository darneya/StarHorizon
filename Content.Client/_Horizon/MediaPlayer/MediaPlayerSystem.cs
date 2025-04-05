using System.Linq;
using Content.Shared._Horizon.MediaPlayer;
using Robust.Shared.Prototypes;

namespace Content.Client._Horizon.MediaPlayer;

public sealed class MediaPlayerSystem : SharedMediaPlayerSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = null!;
    [Dependency] private readonly IPrototypeManager _protoManager = null!;
    public List<MediaFilePrototype> MediaFilePrototypes = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MediaPlayerComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeAllEvent<RepeatMessage>(HandleRepeatMessage);

        _protoManager.PrototypesReloaded += OnProtoReload;
        MediaFilePrototypes = _protoManager.EnumeratePrototypes<MediaFilePrototype>()
            .OrderBy(x => x.ID)
            .ToList();

        _protoManager.PrototypesReloaded += _ =>
        {
            MediaFilePrototypes = _protoManager.EnumeratePrototypes<MediaFilePrototype>()
                .OrderBy(x => x.ID)
                .ToList();
        };
    }

    private void OnProtoReload(PrototypesReloadedEventArgs obj)
    {
        if (!obj.WasModified<MediaFilePrototype>())
            return;

        var query = AllEntityQuery<MediaPlayerComponent, UserInterfaceComponent>();

        while (query.MoveNext(out var uid, out _, out var ui))
        {
            if (!_uiSystem.TryGetOpenUi<MediaPlayerBoundUserInterface>((uid, ui), MediaPlayerUIKey.Key, out var bui))
                continue;

            bui.PopulateMediaList(MediaFilePrototypes);
        }
    }

    private void OnAfterAutoHandleState(Entity<MediaPlayerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if(_uiSystem.TryGetOpenUi<MediaPlayerBoundUserInterface>(ent.Owner, MediaPlayerUIKey.Key, out var bui))
            bui.Reload();
    }

    private void HandleRepeatMessage(RepeatMessage args)
    {
        var owner = GetEntity(args.Entity);
        if(_uiSystem.TryGetOpenUi<MediaPlayerBoundUserInterface>(owner, MediaPlayerUIKey.Key, out var bui))
            bui.HandleTitleChange(args.Id);
    }
}
