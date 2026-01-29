using Content.Shared._Horizon.Weapons.Ranged.Overheat;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Horizon.Weapons.Ranged.Overheat;

/// <summary>
/// Попауты и урон при перегреве
/// </summary>
public sealed class GunOverheatSystem : SharedGunOverheatSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    protected override void OnOverheated(Entity<GunOverheatComponent> ent, EntityUid? user)
    {
        base.OnOverheated(ent, user);

        if (user != null && ent.Comp.TouchDamage != null)
        {
            // Проверяем на изольки
            _inventory.TryGetInventoryEntity<DamageOnInteractProtectionComponent>(user.Value, out var protectiveEntity);

            if (protectiveEntity.Comp == null && TryComp<DamageOnInteractProtectionComponent>(user.Value, out var protectiveComp))
                protectiveEntity = (user.Value, protectiveComp);

            // Если есть, не наносим урон
            if (protectiveEntity.Comp == null)
            {
                _damageable.TryChangeDamage(user.Value, ent.Comp.TouchDamage, origin: ent);

                if (ent.Comp.TouchSound != null)
                    _audio.PlayPvs(ent.Comp.TouchSound, ent);

                _popup.PopupEntity(Loc.GetString("gun-overheat-touch"), ent, user.Value);
            }
        }

        _popup.PopupEntity(Loc.GetString("gun-overheat-blocked"), ent);
    }

    protected override void OnOverheatPopup(Entity<GunOverheatComponent> ent)
    {
        base.OnOverheatPopup(ent);
        _popup.PopupEntity(Loc.GetString("gun-overheat-blocked"), ent);
    }

    protected override void OnCooledDown(Entity<GunOverheatComponent> ent)
    {
        base.OnCooledDown(ent);
        _popup.PopupEntity(Loc.GetString("gun-overheat-cooled"), ent);
    }

}
