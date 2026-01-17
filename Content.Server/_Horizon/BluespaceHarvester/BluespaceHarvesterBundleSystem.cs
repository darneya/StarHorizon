using Content.Shared.Destructible;
using Content.Shared.Storage.Components;
using Robust.Shared.Random;
using Robust.Shared.Log;

namespace Content.Server._Horizon.BluespaceHarvester;

public sealed class BluespaceHarvesterBundleSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("TEST.bluespaceHarvester.bundle");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BluespaceHarvesterBundleComponent, StorageBeforeOpenEvent>(OnOpen);
        SubscribeLocalEvent<BluespaceHarvesterBundleComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnOpen(Entity<BluespaceHarvesterBundleComponent> bundle, ref StorageBeforeOpenEvent args)
    {
        _sawmill.Debug($"Bluespace harvester bundle {ToPrettyString(bundle.Owner)} opened, creating loot");
        CreateLoot(bundle);
    }

    private void OnDestruction(Entity<BluespaceHarvesterBundleComponent> bundle, ref DestructionEventArgs args)
    {
        _sawmill.Info($"Bluespace harvester bundle {ToPrettyString(bundle.Owner)} destroyed, creating loot");
        CreateLoot(bundle);
    }

    private void CreateLoot(Entity<BluespaceHarvesterBundleComponent> bundle)
    {
        if (bundle.Comp.Spawned)
        {
            _sawmill.Debug($"Bluespace harvester bundle {ToPrettyString(bundle.Owner)} already spawned loot, skipping");
            return;
        }

        var content = _random.Pick(bundle.Comp.Contents);
        var position = Transform(bundle.Owner).Coordinates;

        _sawmill.Info($"Bluespace harvester bundle {ToPrettyString(bundle.Owner)} creating {content.Amount}x {content.PrototypeId}");

        for (var i = 0; i < content.Amount; i++)
        {
            _sawmill.Debug($"Bluespace harvester bundle {ToPrettyString(bundle.Owner)} spawning {content.PrototypeId} ({i + 1}/{content.Amount})");
            Spawn(content.PrototypeId, position);
        }

        bundle.Comp.Spawned = true;
    }
}
