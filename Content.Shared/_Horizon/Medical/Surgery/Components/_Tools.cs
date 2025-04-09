using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Medical.Surgery.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryToolComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Speed = 1;

    [DataField, AutoNetworkedField]
    public float SuccessRate = 1f;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? StartSound;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? EndSound;
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class OperatingTableComponent : Component;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class BoneGelComponent : Component, ISurgeryToolComponent
{
    public string ToolName => Loc.GetString("BoneGelComponent");
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class BoneSawComponent : Component, ISurgeryToolComponent
{
    public string ToolName => Loc.GetString("BoneSawComponent");
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class BoneSetterComponent : Component, ISurgeryToolComponent
{
    public string ToolName => Loc.GetString("BoneSetterComponent");
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class CauteryComponent : Component, ISurgeryToolComponent
{
    public string ToolName => Loc.GetString("CauteryComponent");
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class HemostatComponent : Component, ISurgeryToolComponent
{
    public string ToolName => Loc.GetString("HemostatComponent");
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class RetractComponent : Component, ISurgeryToolComponent
{
    public string ToolName => Loc.GetString("RetractComponent");
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class ScalpelComponent : Component, ISurgeryToolComponent
{
    public string ToolName => Loc.GetString("ScalpelComponent");
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgicalDrillComponent : Component, ISurgeryToolComponent
{
    public string ToolName => Loc.GetString("SurgicalDrillComponent");
}
