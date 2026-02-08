namespace Content.Server._Horizon._Fractions.AnCo.Weapons.Components;

[RegisterComponent]
public sealed partial class AnCoRangedWeaponSpreadModifiersComponent : Component
{
    [DataField]
    public float Modifier = 1f;
}
