using System.Linq;
using Content.Client.Medical.CrewMonitoring;
using Content.Shared.Medical.CrewMonitoring;
using Robust.Client.UserInterface;

namespace Content.Client._Horizon.Medical.CrewMonitoring.Brigmedic;

public sealed class BrigmedicCrewMonitoringBoundUserInterface(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private CrewMonitoringWindow? _menu;

    protected override void Open()
    {
        base.Open();
        EntityUid? gridUid = null;
        var stationName = string.Empty;

        if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform))
        {
            gridUid = xform.GridUid;

            if (EntMan.TryGetComponent<MetaDataComponent>(gridUid, out var metaData))
            {
                stationName = metaData.EntityName;
            }
        }

        _menu = this.CreateWindow<CrewMonitoringWindow>();
        _menu.Set(stationName, gridUid);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        switch (state)
        {
            case CrewMonitoringState st:
                EntMan.TryGetComponent<TransformComponent>(Owner, out var xform);
                var securityDepartmentSensors = st.Sensors
                    .Where(sensor => sensor.JobDepartments.Contains("Security"))
                    .ToList();
                _menu?.ShowSensors(securityDepartmentSensors, Owner, xform?.Coordinates);
                break;
        }
    }
}
