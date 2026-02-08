using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server._Horizon._Fractions.AnCo.Weapons;

[ByRefEvent]
public record struct AnCoGetRecoilModifiersEvent(GunComponent Gun)
{
    public float Modifier = 1f;
}
