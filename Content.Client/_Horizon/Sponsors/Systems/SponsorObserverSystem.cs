using Content.Shared._Horizon.Sponsors.Systems;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client._Horizon.Sponsors.Systems;

public sealed class SponsorObserverSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        var entity = args.Entity;

        if (!EntityManager.TryGetComponent<MetaDataComponent>(entity, out var metaData) ||
            metaData.EntityPrototype == null ||
            !_prototypeManager.TryIndex<EntityPrototype>(metaData.EntityPrototype.ID, out var prototype) ||
            prototype.ID is not ("MobObserver" or "AdminObserver"))
            return;

        if (!EntityManager.TryGetComponent<SpriteComponent>(entity, out var spriteComponent))
            return;

        var spriteOverrideSystem = EntityManager.EntitySysManager.GetEntitySystem<SpriteOverrideSystem>();
        var spriteSpecifier = spriteOverrideSystem.GetSpriteForPlayer(args.Player.Name);
        if (spriteSpecifier == null)
            return;

        var sprite = (entity, spriteComponent);

        switch (spriteSpecifier)
        {
            case SpriteSpecifier.Texture textureSpecifier:
                _spriteSystem.LayerSetSprite(sprite, 0, textureSpecifier);
                break;

            case SpriteSpecifier.Rsi rsiSpecifier:
                ApplyRsiOverride(sprite, rsiSpecifier);
                break;
        }
    }

    private void ApplyRsiOverride(Entity<SpriteComponent?> sprite, SpriteSpecifier.Rsi rsiSpecifier)
    {
        var path = SpriteSpecifierSerializer.TextureRoot / rsiSpecifier.RsiPath;
        if (!_resCache.TryGetResource<RSIResource>(path, out var rsiResource) || rsiResource.RSI == null)
            return;

        var rsi = rsiResource.RSI;
        var stateId = rsiSpecifier.RsiState;
        if (!rsi.TryGetState(stateId, out _))
            return;

        _spriteSystem.LayerSetRsi(sprite, 0, rsi, stateId);
    }
}
