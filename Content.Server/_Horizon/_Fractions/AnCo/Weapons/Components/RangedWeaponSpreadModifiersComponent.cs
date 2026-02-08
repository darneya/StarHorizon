namespace Content.Server._Horizon._Fractions.AnCo.Weapons.Components;

[RegisterComponent]
public sealed partial class RangedWeaponSpreadModifiersComponent : Component
{
    [DataField]
    public float Modifier = 1f;
}
