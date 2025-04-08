using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Aavikko.Medical.Surgery.Effects.Step;

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
    public string ToolName => "костный гель";
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class BoneSawComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "пила для костей";
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class BoneSetterComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "костоправ";
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class CauteryComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "прижигание";
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class HemostatComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "кровоостанавливающий зажим";
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class RetractorComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "ретрактор";
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class ScalpelComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "скальпель";
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgicalDrillComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "хирургическая дрель";
}
