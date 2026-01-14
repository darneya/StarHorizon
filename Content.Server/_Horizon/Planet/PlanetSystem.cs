using Content.Server.Atmos.EntitySystems;
using Content.Server.GameTicking.Events;
using Content.Server.Parallax;
using Content.Shared._Horizon.Planet;
using Content.Shared.Parallax.Biomes;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Horizon.Planet;

public sealed class PlanetSystem : EntitySystem
{

    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;

    private List<(Vector2i, Tile)> _setTiles = new();
    public Dictionary<ProtoId<PlanetPrototype>, EntityUid> LoadedPlanets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarted);
    }

    private void OnRoundStarted(RoundStartingEvent ev)
    {
        foreach (var proto in _proto.EnumeratePrototypes<PlanetPrototype>())
        {
            if (!proto.SpawnRoundstart)
                continue;

            if (proto.MapPath is { } path)
            {
                LoadPlanetWithMap(proto.ID, path.CanonPath);
            }
            else
                SpawnPlanet(proto.ID);
        }
    }

    /// <summary>
    /// Создаёт планету из прототипа
    /// </summary>
    public EntityUid SpawnPlanet(ProtoId<PlanetPrototype> id, bool runMapInit = true)
    {
        var planet = _proto.Index(id);

        var map = _map.CreateMap(out _, runMapInit: runMapInit);
        _biome.EnsurePlanet(map, _proto.Index(planet.Biome), mapLight: planet.MapLight);

        // add each marker layer
        var biome = Comp<BiomeComponent>(map);
        foreach (var layer in planet.BiomeMarkerLayers)
        {
            _biome.AddMarkerLayer(map, biome, layer);
        }

        if (planet.AddedComponents is { } added)
            EntityManager.AddComponents(map, added);

        _atmos.SetMapAtmosphere(map, false, planet.Atmosphere);

        _meta.SetEntityName(map, Loc.GetString(planet.MapName));

        LoadedPlanets[id] = map;
        return map;
    }

    /// <summary>
    /// Спавнит планету с загрузкой определённой карты
    /// </summary>
    public EntityUid? LoadPlanetWithMap(ProtoId<PlanetPrototype> id, string path)
    {
        var map = SpawnPlanet(id, runMapInit: false);
        var mapId = Comp<MapComponent>(map).MapId;

        if (!_mapLoader.TryLoadGrid(mapId, new ResPath(path), out var grids))
        {
            Log.Error($"Failed to load planet grid {path} for planet {id}!");
            return null;
        }

        if (grids.HasValue)
        {
            var gridUid = grids.Value;
            _setTiles.Clear();
            var aabb = Comp<MapGridComponent>(gridUid).LocalAABB;
            _biome.ReserveTiles(map, aabb.Enlarged(0.2f), _setTiles);
        }
        else
        {
            Log.Error("Grid not found for this map.");
        }

        _map.InitializeMap(map);

        LoadedPlanets[id] = map;
        return map;
    }
}
