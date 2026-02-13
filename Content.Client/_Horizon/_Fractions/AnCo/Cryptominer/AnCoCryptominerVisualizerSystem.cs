using Content.Shared._Horizon._Fractions.AnCo.Cryptominer;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client._Horizon._Fractions.AnCo.Cryptominer;

public sealed class AnCoCryptominerVisualizerSystem : VisualizerSystem<AnCoCryptominerComponent>
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;

    private const string OpenAnimationKey = "cryptominer_open";
    private const string CloseAnimationKey = "cryptominer_close";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnCoCryptominerComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    protected override void OnAppearanceChange(EntityUid uid, AnCoCryptominerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<CryptominerState>(uid, CryptominerVisuals.State, out var state, args.Component))
            state = CryptominerState.Off;

        if (!AppearanceSystem.TryGetData<bool>(uid, CryptominerVisuals.IsVentOpen, out var isVentOpen, args.Component))
            isVentOpen = false;

        if (!AppearanceSystem.TryGetData<int>(uid, CryptominerVisuals.DiskCount, out var diskCount, args.Component))
            diskCount = 0;

        // Update base layers based on state
        UpdateLevelLayer(args.Sprite, state);

        // Handle lock/vent animations
        HandleVentState(uid, args.Sprite, state, isVentOpen, component);

        // Update disk layer based on count
        UpdateDiskLayer(args.Sprite, diskCount);
    }

    private void UpdateLevelLayer(SpriteComponent sprite, CryptominerState state)
    {
        // Update level overlay based on state
        // level-1 = on/normal, level-2 = warning, level-3 = overheat, level-4 = critical
        if (!sprite.LayerMapTryGet(CryptominerVisualLayers.Level, out var levelLayer))
            return;

        if (state == CryptominerState.Off || state == CryptominerState.NoAtmosphere || state == CryptominerState.NoDisks)
        {
            sprite.LayerSetVisible(levelLayer, false);
            return;
        }

        sprite.LayerSetVisible(levelLayer, true);
        var levelState = state switch
        {
            CryptominerState.Normal => "level-1",
            CryptominerState.Warning => "level-2",
            CryptominerState.Overheat => "level-3",
            CryptominerState.Critical => "level-4",
            _ => "level-1"
        };
        sprite.LayerSetState(levelLayer, levelState);
    }

    private void HandleVentState(EntityUid uid, SpriteComponent sprite, CryptominerState state, bool isVentOpen, AnCoCryptominerComponent component)
    {
        if (!sprite.LayerMapTryGet(CryptominerVisualLayers.Lock, out var lockLayer))
            return;

        var wasVentOpen = component.IsVentOpen;
        component.IsVentOpen = isVentOpen;

        // Vent should be closed for Off, Normal, Warning
        // Vent should be open for Overheat, Critical
        if (isVentOpen && !wasVentOpen)
        {
            // Play open animation
            PlayOpenAnimation(uid, sprite, lockLayer);
        }
        else if (!isVentOpen && wasVentOpen)
        {
            // Play close animation
            PlayCloseAnimation(uid, sprite, lockLayer);
        }
        else if (!isVentOpen)
        {
            // Ensure lock is visible when vent is closed
            sprite.LayerSetVisible(lockLayer, true);
            sprite.LayerSetState(lockLayer, "lock");
        }
        else
        {
            // Ensure lock is hidden when vent is open
            sprite.LayerSetVisible(lockLayer, false);
        }
    }

    private void PlayOpenAnimation(EntityUid uid, SpriteComponent sprite, int lockLayer)
    {
        if (_animationPlayer.HasRunningAnimation(uid, OpenAnimationKey))
            return;

        // First hide the lock, then switch to open animation
        sprite.LayerSetVisible(lockLayer, false);
        sprite.LayerSetState(lockLayer, "open");
        sprite.LayerSetAutoAnimated(lockLayer, true);
        sprite.LayerSetVisible(lockLayer, true);

        // Animation length: 10 frames * 0.2 sec = 2 seconds
        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(2.0),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Visible),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(true, 0f),
                        new AnimationTrackProperty.KeyFrame(true, 2.0f)
                    }
                }
            }
        };

        _animationPlayer.Play(uid, animation, OpenAnimationKey);
    }

    private void PlayCloseAnimation(EntityUid uid, SpriteComponent sprite, int lockLayer)
    {
        if (_animationPlayer.HasRunningAnimation(uid, CloseAnimationKey))
            return;

        sprite.LayerSetVisible(lockLayer, true);
        sprite.LayerSetState(lockLayer, "close");
        sprite.LayerSetAutoAnimated(lockLayer, true);

        // Animation length: 10 frames * 0.2 sec = 2 seconds
        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(2.0),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Visible),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(true, 0f),
                        new AnimationTrackProperty.KeyFrame(true, 2.0f)
                    }
                }
            }
        };

        _animationPlayer.Play(uid, animation, CloseAnimationKey);
    }

    private void OnAnimationCompleted(EntityUid uid, AnCoCryptominerComponent component, AnimationCompletedEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!sprite.LayerMapTryGet(CryptominerVisualLayers.Lock, out var lockLayer))
            return;

        if (args.Key == OpenAnimationKey)
        {
            // Hide lock layer after open animation
            sprite.LayerSetVisible(lockLayer, false);
        }
        else if (args.Key == CloseAnimationKey)
        {
            // Show lock state after close animation
            sprite.LayerSetState(lockLayer, "lock");
            sprite.LayerSetAutoAnimated(lockLayer, false);
        }
    }

    private void UpdateDiskLayer(SpriteComponent sprite, int diskCount)
    {
        if (!sprite.LayerMapTryGet(CryptominerVisualLayers.Disks, out var diskLayer))
            return;

        if (diskCount <= 0)
        {
            sprite.LayerSetVisible(diskLayer, false);
            return;
        }

        sprite.LayerSetVisible(diskLayer, true);

        // disk-1 for 1 disk, disk-2 for 2 disks, etc.
        var diskState = $"disk-{Math.Clamp(diskCount, 1, 4)}";
        sprite.LayerSetState(diskLayer, diskState);
    }
}
