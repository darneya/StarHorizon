using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.NightVision;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(NightVisionSystem))]
public sealed partial class NightVisionComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isOn"), AutoNetworkedField]
    public bool IsNightVision;

    [DataField("color")]
    public Color NightVisionColor = Color.FromHex("#9c9c9c");

    [DataField]
    public bool IsToggle = false;

    [DataField]
    public EntityUid? ActionContainer;

    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool DrawShadows = false;

    [Access(Other = AccessPermissions.ReadWriteExecute)]
    public bool GraceFrame = false;

    [DataField("transitionDuration")]
    public float TransitionDuration = 0.3f;
}

public sealed partial class NVInstantActionEvent : InstantActionEvent { }
