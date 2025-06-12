namespace Content.Shared.Aavikko.Weapons.Ranged.Components;

[RegisterComponent]
public sealed partial class GunModuleComponent : Component
{
    // Модификатор урона
    [DataField]
    public float DamageModifier = 1.0f;

    // Модификатор скорострельности
    [DataField]
    public float FireRateModifier = 1.0f;
}
