// Maded by Gorox. Discord - smeshinka112
using Content.Server._Horizon.XenoBiology.Components;
using Content.Server._Horizon.XenoFood.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;

namespace Content.Server._Horizon.XenoBiology.Systems;

public sealed class XenoBiologySystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XenoBiologyComponent, MeleeHitEvent>(OnSlimeAttack);
    }

    private void OnSlimeAttack(EntityUid uid, XenoBiologyComponent component, ref MeleeHitEvent args)
    {
        foreach (var hitEntity in args.HitEntities)
        {
            if (EntityManager.HasComponent<XenoFoodComponent>(hitEntity))
            {
                if (_mobState.IsIncapacitated(hitEntity))
                    return;

                component.Points += component.PointsPerAttack;
                break;
            }
        }
    }

    public override void Update(float frameTime)
    {
        var xenoQuery = EntityQueryEnumerator<XenoBiologyComponent, TransformComponent>();
        while (xenoQuery.MoveNext(out var uid, out var component, out _))
        {
            var prototype = MetaData(uid).EntityPrototype?.ID;

            if (component.Points >= component.PointsThreshold)
            {
                if (TryComp<MindContainerComponent>(uid, out var mindContainer) && mindContainer.HasMind)
                {
                    _polymorph.PolymorphEntity(uid, component.OnMind);
                }

                if (_robustRandom.Prob(component.Mutationchance))
                {
                    Spawn(component.Mutagen, Transform(uid).Coordinates);
                }
                else
                {
                    Spawn(prototype, Transform(uid).Coordinates);
                }

                if (_robustRandom.Prob(component.Mutationchance))
                {
                    Spawn(component.Mutagen, Transform(uid).Coordinates);
                }
                else
                {
                    Spawn(prototype, Transform(uid).Coordinates);
                }

                if (_robustRandom.Prob(component.Mutationchance))
                {
                    Spawn(component.Mutagen, Transform(uid).Coordinates);
                }
                else
                {
                    Spawn(prototype, Transform(uid).Coordinates);
                }

                EntityManager.DeleteEntity(uid);
                return;
            }

            else if (component.Points > 0 && _robustRandom.Prob(0.001f))
            {
                component.Points -= 1;
            }
        }
    }
}
