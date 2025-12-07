using Content.Shared._Horizon.RemoteControl.Components;
using Content.Shared.Mech;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.DoAfter;
using Content.Shared.Mech.EntitySystems;

namespace Content.Shared._Horizon.RemoteControl.Systems;

public abstract class SharedRemotePilotSystem : EntitySystem
{

    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly RemoteControlSystem _remoteControlSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RemotePilotComponent, OnPilotEjectEvent>(OnPilotEject);
    }

    private void OnPilotEject(Entity<RemotePilotComponent> pilot, ref OnPilotEjectEvent args)
    {
        _remoteControlSystem.ReturnToBody(pilot.Owner);
    }

    public bool TryCreateRemotePilot(EntityUid host, [NotNullWhen(true)] out EntityUid? pilotUid)
    {
        pilotUid = null;

        if (!TryComp<CanBeTakenUnderControlComponent>(host, out var hostComp))
            return false;

        pilotUid = PredictedSpawnAtPosition(hostComp.RemotePilot, Transform(host).Coordinates);

        //Don't create a pilot in the mech immediately, because we need to call the MechEntryEvent to initialize the UI update for controlling the mech.
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, pilotUid.Value, 0f, new MechEntryEvent(), host, target: host)
        {
            Broadcast = false,
            Hidden = true
        });


        return true;
    }

}
