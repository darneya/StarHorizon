using Content.Server.Station.Components;
using Content.Shared._Horizon.Shuttles.Components;
using Content.Shared.Shuttles.Components;

namespace Content.Server._Horizon.Shuttles.Systems;

/// <summary>
/// Handles automatic linking of coordinate disks to station maps by BecomesStation ID.
/// </summary>
public sealed class AutoLinkCoordinatesDiskSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoLinkCoordinatesDiskComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<AutoLinkCoordinatesDiskComponent> ent, ref ComponentStartup args)
    {
        // Find grid with matching BecomesStation ID and get its map
        var query = EntityQueryEnumerator<BecomesStationComponent, TransformComponent>();

        while (query.MoveNext(out _, out var becomesStation, out var xform))
        {
            if (becomesStation.Id == ent.Comp.TargetStationId)
            {
                // Found the station grid, get its map and set destination
                if (xform.MapUid != null)
                {
                    var coordsComp = EnsureComp<ShuttleDestinationCoordinatesComponent>(ent);
                    coordsComp.Destination = xform.MapUid.Value;
                    Dirty(ent, coordsComp);
                    return;
                }
            }
        }

        // Station not found - this is expected if POI hasn't spawned yet
        // The disk will remain without destination
    }
}
