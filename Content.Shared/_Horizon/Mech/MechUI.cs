using Content.Shared.Mech;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Mech;

[Serializable, NetSerializable]
public sealed class MechGunUiState : BoundUserInterfaceState
{
    public int Capacity;
    public int Shots;
    public float ReloadTime;
    public bool Reloading = false;
    public TimeSpan? ReloadEndTime = null;
}

[Serializable, NetSerializable]
public sealed class MechToolsUiState : BoundUserInterfaceState
{
    public List<NetEntity> Tools;
    public string SelectedTool;

    public MechToolsUiState(List<NetEntity> tools, string selectedTool)
    {
        Tools = tools;
        SelectedTool = selectedTool;
    }
}

[Serializable, NetSerializable]
public sealed class MechGunReloadMessage : MechEquipmentUiMessage
{

    public MechGunReloadMessage(NetEntity equipment)
    {
        Equipment = equipment;
    }
}

[Serializable, NetSerializable]
public sealed class MechToolSetMessage : MechEquipmentUiMessage
{
    public string Tool;

    public MechToolSetMessage(string tool, NetEntity equipment)
    {
        Equipment = equipment;
        Tool = tool;
    }
}
