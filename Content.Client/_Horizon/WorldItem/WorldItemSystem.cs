using System.Diagnostics.CodeAnalysis;
using Content.Shared._Horizon.WorldItem;
using Robust.Client.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;

#pragma warning disable CS8073 // The result of the expression is always the same since a value of this type is never equal to 'null'

namespace Content.Client._Horizon.WorldItem;

/// <summary>
/// Выясняет находиться ли объект в данный момент на полу или нет.
/// </summary>
public sealed class WorldItemSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSys = null!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<WorldItemComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<WorldItemComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WorldItemComponent, ComponentAdd>(OnAdd);
        SubscribeLocalEvent<WorldItemComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<WorldItemComponent, EntParentChangedMessage>(ChangeItemSprite);
    }

    #region Component Events
    private void OnInit(Entity<WorldItemComponent> entity, ref ComponentInit _)
    {
        if (!TryComp<SpriteComponent>(entity.Owner, out var sprite))
            return;

        var layerNumber = 0;
        foreach (var layer in sprite.AllLayers)
        {
            if (layer.RsiState.Name == null)
                continue;

            var state = layer.RsiState.Name;
            entity.Comp.DefaultSpriteStates.Add(layerNumber, state);
            layerNumber++;
        }

        ChangeItemSprite(entity);
    }

    private void OnShutdown(Entity<WorldItemComponent> entity, ref ComponentShutdown _)
    {
        if (!TryComp<SpriteComponent>(entity.Owner, out var sprite))
            return;

        foreach (var (layer, state) in entity.Comp.DefaultSpriteStates)
        {
            sprite.LayerSetState(layer, state);
        }

        ChangeItemSprite(entity);
    }

    private void OnAdd(Entity<WorldItemComponent> entity, ref ComponentAdd _)
    {
        if (!TryComp<SpriteComponent>(entity.Owner, out var sprite))
            return;

        var layerNumber = 0;
        foreach (var layer in sprite.AllLayers)
        {
            if (layer.RsiState.Name == null)
                continue;

            var state = layer.RsiState.Name;
            entity.Comp.DefaultSpriteStates.Add(layerNumber, state);
            layerNumber++;
        }

        ChangeItemSprite(entity);
    }

    private void OnRemove(Entity<WorldItemComponent> entity, ref ComponentRemove _)
    {
        if (!TryComp<SpriteComponent>(entity.Owner, out var sprite))
            return;

        foreach (var (layer, state) in entity.Comp.DefaultSpriteStates)
        {
            sprite.LayerSetState(layer, state);
        }

        ChangeItemSprite(entity);
    }

    #endregion

    private void ChangeItemSprite(Entity<WorldItemComponent> entity, ref EntParentChangedMessage _)
    {
        if (!TryComp<SpriteComponent>(entity.Owner, out var sprite) || entity.Comp.DefaultSpriteStates.Count == 0)
            return;

        if (TryComp<AppearanceComponent>(entity.Owner, out var appearance))
        {
            _appearanceSys.QueueUpdate(entity.Owner, appearance);
            return;
        }

        if (GetWorldState(entity.Owner, out var prefix, out var _))
        {
            foreach (var (layer, state) in entity.Comp.DefaultSpriteStates)
            {
                sprite.LayerSetState(layer, state + prefix);
            }
        }
        else
        {
            foreach (var (layer, state) in entity.Comp.DefaultSpriteStates)
            {
                sprite.LayerSetState(layer, state);
            }
        }
    }
    private void ChangeItemSprite(Entity<WorldItemComponent> entity)
    {
        if (!TryComp<SpriteComponent>(entity.Owner, out var sprite) || entity.Comp.DefaultSpriteStates.Count == 0)
            return;

        if (TryComp<AppearanceComponent>(entity.Owner, out var appearance))
        {
            _appearanceSys.QueueUpdate(entity.Owner, appearance);
            return;
        }

        if (GetWorldState(entity.Owner, out var prefix, out _))
        {
            foreach (var (layer, state) in entity.Comp.DefaultSpriteStates)
            {
                sprite.LayerSetState(layer, state + prefix);
            }
        }
        else
        {
            foreach (var (layer, state) in entity.Comp.DefaultSpriteStates)
            {
                sprite.LayerSetState(layer, state);
            }
        }
    }

    // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    public bool GetWorldState(EntityUid uid, [NotNullWhen(true)] out string? prefix, [NotNullWhen(true)] out Dictionary<int, string>? spriteStates)
    {
        if (!TryComp<WorldItemComponent>(uid, out var worldItem))
        {
            prefix = null;
            spriteStates = null;
            return false;
        }

        spriteStates = worldItem.DefaultSpriteStates;
        prefix = worldItem.Prefix;
        var transform = Transform(uid);
        if (transform == null)
            return false;

        var parent = transform.ParentUid;
        if (parent == null)
            return false;

        return HasComp<BroadphaseComponent>(parent)
               || HasComp<MapComponent>(parent)
               || HasComp<MapGridComponent>(parent);
    }
}
