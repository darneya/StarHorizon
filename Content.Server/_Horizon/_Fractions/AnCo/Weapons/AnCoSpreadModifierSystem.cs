using Content.Server._Horizon._Fractions.AnCo.Weapons.Components;

namespace Content.Server._Horizon._Fractions.AnCo.Weapons;

public sealed class AnCoSpreadModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RangedWeaponSpreadModifiersComponent, AnCoGetRecoilModifiersEvent>(OnGetModifier);
    }

    private void OnGetModifier(Entity<RangedWeaponSpreadModifiersComponent> ent, ref AnCoGetRecoilModifiersEvent args)
    {
        args.Modifier *= ent.Comp.Modifier;
    }
}
