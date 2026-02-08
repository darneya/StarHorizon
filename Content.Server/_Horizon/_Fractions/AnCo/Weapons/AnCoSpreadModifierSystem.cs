namespace Content.Server._Horizon.Weapons;

public sealed class AnCoSpreadModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnCoRangedWeaponSpreadModifiersComponent, AnCoGetRecoilModifiersEvent>(OnGetModifier);
    }

    private void OnGetModifier(Entity<AnCoRangedWeaponSpreadModifiersComponent> ent, ref AnCoGetRecoilModifiersEvent args)
    {
        args.Modifier *= ent.Comp.Modifier;
    }
}
