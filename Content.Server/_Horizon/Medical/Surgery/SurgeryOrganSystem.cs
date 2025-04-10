using Content.Shared._Horizon.Medical.Surgery.Components;
using Content.Shared._Horizon.Medical.Surgery.Events;
using Content.Shared.Damage;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Speech.Muting;

namespace Content.Server._Horizon.Medical.Surgery;
public sealed partial class SurgeryOrganSystem : EntitySystem
{

    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OrganEyesComponent, SurgeryOrganImplantationCompleted>(OnEyeImplanted);
        SubscribeLocalEvent<DamageableComponent, SurgeryOrganImplantationCompleted>(OnOrganImplanted);
        SubscribeLocalEvent<OrganTongueComponent, SurgeryOrganImplantationCompleted>(OnTongueImplanted);

        SubscribeLocalEvent<OrganEyesComponent, SurgeryOrganExtractCompleted>(OnOrganExtracted);
        SubscribeLocalEvent<OrganTongueComponent, SurgeryOrganExtractCompleted>(OnTongueExtracted);
        SubscribeLocalEvent<DamageableComponent, SurgeryOrganExtractCompleted>(OnOrganExtracted);
    }

    private void OnOrganExtracted(Entity<DamageableComponent> ent, ref SurgeryOrganExtractCompleted args)
    {
        if (!TryComp<OrganDamageComponent>(ent.Owner, out var damageRule)
         || damageRule.Damage is null
         || !TryComp<DamageableComponent>(args.Body, out var bodyDamageable)) return;

        var change = _damageableSystem.TryChangeDamage(args.Body, damageRule.Damage.Invert(), true, false, bodyDamageable);
        if (change is not null)
            _damageableSystem.TryChangeDamage(ent.Owner, change.Invert(), true, false, ent.Comp);
    }

    private void OnTongueExtracted(Entity<OrganTongueComponent> ent, ref SurgeryOrganExtractCompleted args)
    {
        if (HasComp<MutedComponent>(args.Body))
            ent.Comp.IsMuted = true;
        else
            AddComp<MutedComponent>(args.Body);
    }

    private void OnOrganExtracted(Entity<OrganEyesComponent> ent, ref SurgeryOrganExtractCompleted args)
    {
        if (!TryComp<BlindableComponent>(args.Body, out var blindable))
            return;

        ent.Comp.EyeDamage = blindable.MaxDamage;
        ent.Comp.MinDamage = blindable.MinDamage;
        _blindable.UpdateIsBlind((args.Body, blindable));
    }

    private void OnOrganImplanted(Entity<DamageableComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (!TryComp<DamageableComponent>(args.Body, out var bodyDamageable)) return;

        var change = _damageableSystem.TryChangeDamage(args.Body, ent.Comp.Damage, true, false, bodyDamageable);
        if (change is not null)
            _damageableSystem.TryChangeDamage(ent.Owner, change.Invert(), true, false, ent.Comp);
    }

    private void OnEyeImplanted(Entity<OrganEyesComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (!TryComp<BlindableComponent>(args.Body, out var blindable))
            return;

        _blindable.SetMinDamage((args.Body, blindable), ent.Comp.MinDamage ?? 0);
        _blindable.AdjustEyeDamage((args.Body, blindable), (ent.Comp.EyeDamage ?? 0) - blindable.MaxDamage);
    }

    private void OnTongueImplanted(Entity<OrganTongueComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (HasComp<MutedComponent>(args.Body))
            RemComp<MutedComponent>(args.Body);
    }
}
