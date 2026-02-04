using Content.Server._Horizon.SponsorManager;
using Content.Shared._Horizon.GhostSprites;
using Content.Shared.Ghost;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Horizon.GhostSprites;

/// <summary>
/// Server-side system that handles ghost sprite change requests.
/// All validation and state changes happen here.
/// </summary>
public sealed class GhostSpriteSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SponsorManager.SponsorManager _sponsorManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestGhostSpritesEvent>(OnRequestGhostSprites);
        SubscribeNetworkEvent<ChangeGhostSpriteRequestEvent>(OnChangeGhostSpriteRequest);
    }

    private void OnRequestGhostSprites(RequestGhostSpritesEvent msg, EntitySessionEventArgs args)
    {
        var playerName = args.SenderSession.Name;
        var isSponsor = _sponsorManager.IsSponsor(playerName);

        var sprites = new List<ProtoId<GhostSpritePrototype>>();

        foreach (var proto in _prototypeManager.EnumeratePrototypes<GhostSpritePrototype>())
        {
            // Skip sponsor-only sprites for non-sponsors
            if (proto.SponsorOnly && !isSponsor)
                continue;

            sprites.Add(proto.ID);
        }

        var response = new GhostSpritesResponseEvent(sprites);
        RaiseNetworkEvent(response, args.SenderSession);
    }

    private void OnChangeGhostSpriteRequest(ChangeGhostSpriteRequestEvent msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;

        // Validate: player must have an attached entity
        if (session.AttachedEntity is not { Valid: true } playerEntity)
            return;

        // Validate: player must be a ghost
        if (!TryComp<GhostComponent>(playerEntity, out _))
            return;

        // Validate: sprite prototype must exist
        if (!_prototypeManager.TryIndex<GhostSpritePrototype>(msg.SpriteId, out var prototype))
            return;

        // Validate: sponsor-only sprites require sponsor status
        if (prototype.SponsorOnly && !_sponsorManager.IsSponsor(session.Name))
            return;

        // Apply the sprite change
        var spriteComp = EnsureComp<GhostSpriteComponent>(playerEntity);
        spriteComp.SelectedSprite = msg.SpriteId;
        Dirty(playerEntity, spriteComp);

        // Broadcast the change to ALL clients so everyone sees the updated sprite
        var response = new GhostSpriteChangedEvent(GetNetEntity(playerEntity), msg.SpriteId);
        RaiseNetworkEvent(response);
    }

    /// <summary>
    /// Sets the ghost sprite for an entity. Can be called by other systems.
    /// </summary>
    public void SetGhostSprite(EntityUid ghost, ProtoId<GhostSpritePrototype> spriteId)
    {
        if (!HasComp<GhostComponent>(ghost))
            return;

        if (!_prototypeManager.TryIndex<GhostSpritePrototype>(spriteId, out _))
            return;

        var spriteComp = EnsureComp<GhostSpriteComponent>(ghost);
        spriteComp.SelectedSprite = spriteId;
        Dirty(ghost, spriteComp);

        // Broadcast the change to ALL clients
        var response = new GhostSpriteChangedEvent(GetNetEntity(ghost), spriteId);
        RaiseNetworkEvent(response);
    }
}
