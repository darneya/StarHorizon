namespace Content.Server._Horizon.AnCoAutoGuidedBullet.Components;

[RegisterComponent]
public sealed partial class AnCoGunAutoTargetComponent : Component
{
    [DataField]
    public float Range = 2.5f;

    [DataField]
    public float TurnSpeed = 25.0f;
}
