// Maded by Gorox. Discord - smeshinka112
using Content.Shared._Horizon.XenoPotion.Components;
using Content.Shared._Horizon.XenoPotionEffected.Components;
using Content.Server.Atmos.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Interaction;
using Content.Shared.Clothing;

namespace Content.Server._Horizon.XenoPotion;

public sealed class XenoPotionSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoPotionComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(EntityUid uid, XenoPotionComponent component, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target != null && component.Effect == "Speed" && !EntityManager.HasComponent<XenoPotionEffectedComponent>(args.Target.Value))
        {
            if (args.Target != null && TryComp<ClothingSpeedModifierComponent>(args.Target.Value, out var speedComp))
            {
                var meta = MetaData(args.Target.Value);
                var name = meta.EntityName;

                if (speedComp.SprintModifier > 1.0)
                    return;

                EnsureComp<XenoPotionEffectedComponent>(args.Target.Value, out XenoPotionEffectedComponent? color);

                _metaData.SetEntityName(args.Target.Value, Loc.GetString("potion-speed-name-prefix", ("target", name)));

                EntityManager.RemoveComponent<ClothingSpeedModifierComponent>(args.Target.Value);

                color.Color = component.Color;

                EntityManager.DeleteEntity(args.Used);
            }
        }

        else if (args.Target != null && component.Effect == "Pressure" && !EntityManager.HasComponent<XenoPotionEffectedComponent>(args.Target.Value))
        {
            if (args.Target != null && !EntityManager.HasComponent<PressureProtectionComponent>(args.Target.Value) && EntityManager.HasComponent<ClothingComponent>(args.Target.Value))
            {
                var meta = MetaData(args.Target.Value);
                var name = meta.EntityName;

                EnsureComp<XenoPotionEffectedComponent>(args.Target.Value, out XenoPotionEffectedComponent? color);

                _metaData.SetEntityName(args.Target.Value, Loc.GetString("potion-pressure-name-prefix", ("target", name)));

                EnsureComp<PressureProtectionComponent>(args.Target.Value, out PressureProtectionComponent pressure);

                color.Color = component.Color;

                pressure.LowPressureMultiplier = 1000f;

                EntityManager.DeleteEntity(args.Used);
            }
        }

        args.Handled = true;
    }
}
