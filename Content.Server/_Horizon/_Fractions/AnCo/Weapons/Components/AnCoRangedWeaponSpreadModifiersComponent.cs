namespace Content.Server._Horizon.Weapons;

[RegisterComponent]
public sealed partial class AnCoRangedWeaponSpreadModifiersComponent : Component
{
    [DataField]
    public float Modifier = 1f;
}
