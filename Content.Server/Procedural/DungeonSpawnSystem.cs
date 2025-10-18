using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Procedural;
using Robust.Shared.Random;

namespace Content.Server.Procedural;

public sealed class DungeonSpawnSystem : EntitySystem
{
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DungeonSpawnComponent, MapInitEvent>(OnDungeonSpawnMapInit);
    }

    private void OnDungeonSpawnMapInit(EntityUid uid, DungeonSpawnComponent component, MapInitEvent args)
    {
        var xform = Transform(uid);
        if (xform.GridUid != null)
        {
            if (!_prototype.TryIndex<DungeonConfigPrototype>(component.DungeonPreset, out var dungeonConfig))
            {
                Log.Error($"Dungeon config '{component.DungeonPreset}' not found for {ToPrettyString(uid)}");
                QueueDel(uid);
                return;
            }

            var mapGrid = Comp<MapGridComponent>(xform.GridUid.Value);

            int seed = component.Seed ?? _random.Next();

            var tilePosition = _maps.LocalToTile(xform.GridUid.Value, mapGrid, xform.Coordinates);

            _dungeon.GenerateDungeon(
                dungeonConfig,
                component.DungeonPreset,
                xform.GridUid.Value,
                mapGrid,
                tilePosition,
                seed);
        }
        else
        {
            Log.Error($"Dungeon spawn marker {ToPrettyString(uid)} is not on a grid");
        }
        QueueDel(uid);
    }
}