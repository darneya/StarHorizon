using Content.Shared.Examine;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._Horizon.Weapons.Ranged.Overheat;

public abstract class SharedGunOverheatSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunOverheatComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<GunOverheatComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<GunOverheatComponent, ExaminedEvent>(OnExamined);
    }

    private void OnGunShot(Entity<GunOverheatComponent> ent, ref GunShotEvent args)
    {
        AddHeat(ent, ent.Comp.HeatPerShot, args.User);
    }

    private void OnShotAttempted(Entity<GunOverheatComponent> ent, ref ShotAttemptedEvent args)
    {
        if (!ent.Comp.Overheated)
            return;

        args.Cancel();
    }

    private void OnExamined(Entity<GunOverheatComponent> ent, ref ExaminedEvent args)
    {
        var heatPercent = (int)(ent.Comp.CurrentHeat * 100);

        if (ent.Comp.Overheated)
        {
            args.PushMarkup(Loc.GetString("gun-overheat-examine-overheated"));
        }
        else if (heatPercent > 0)
        {
            args.PushMarkup(Loc.GetString("gun-overheat-examine-heat", ("heat", heatPercent)));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GunOverheatComponent>();

        while (query.MoveNext(out var uid, out var overheat))
        {
            if (overheat.CurrentHeat > 0f)
            {
                overheat.CurrentHeat = Math.Max(0f, overheat.CurrentHeat - overheat.CooldownRate * frameTime);
                Dirty(uid, overheat);
            }

            if (!overheat.Overheated)
                continue;

            if (overheat.CurrentHeat <= overheat.UnlockThreshold)
            {
                overheat.Overheated = false;
                Dirty(uid, overheat);
                OnCooledDown((uid, overheat));
            }
            else
            {
                overheat.NextPopupTime -= frameTime;
                if (overheat.NextPopupTime <= 0f)
                {
                    overheat.NextPopupTime = overheat.PopupInterval;
                    OnOverheatPopup((uid, overheat));
                }
            }
        }
    }

    protected virtual void OnOverheatPopup(Entity<GunOverheatComponent> ent)
    {
    }

    public void AddHeat(Entity<GunOverheatComponent> ent, float amount, EntityUid? user = null)
    {
        ent.Comp.CurrentHeat = Math.Min(1f, ent.Comp.CurrentHeat + amount);

        if (ent.Comp.CurrentHeat >= ent.Comp.OverheatThreshold && !ent.Comp.Overheated)
        {
            ent.Comp.Overheated = true;
            ent.Comp.NextPopupTime = ent.Comp.PopupInterval;
            OnOverheated(ent, user);
        }

        Dirty(ent);
    }

    protected virtual void OnOverheated(Entity<GunOverheatComponent> ent, EntityUid? user)
    {
    }

    protected virtual void OnCooledDown(Entity<GunOverheatComponent> ent)
    {
    }

}
