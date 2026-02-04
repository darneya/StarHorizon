using Content.Client.Weapons.Ranged.Components;
using Content.Shared._Horizon.Weapons.Ranged.Overheat;
using Robust.Client.GameObjects;

namespace Content.Client._Horizon.Weapons.Ranged.Overheat;

/// <summary>
/// Добавляет визуальный эффект покраснения при перегреве.
/// </summary>
public sealed class GunOverheatSystem : SharedGunOverheatSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GunOverheatComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var overheat, out var sprite))
        {
            UpdateOverheatVisuals(uid, overheat, sprite);
        }
    }

    private void UpdateOverheatVisuals(EntityUid uid, GunOverheatComponent overheat, SpriteComponent sprite)
    {
        if (!overheat.VisualOverheat)
            return;

        var heat = overheat.CurrentHeat;
        var color = Color.InterpolateBetween(Color.White, Color.Red, heat);
        _sprite.SetColor((uid, sprite), color);

        // Включаем unshaded слои при перегреве
        var showUnshaded = overheat.Overheated;

        if (_sprite.LayerMapTryGet((uid, sprite), GunVisualLayers.BaseUnshaded, out _, false))
            _sprite.LayerSetVisible((uid, sprite), GunVisualLayers.BaseUnshaded, showUnshaded);

        if (_sprite.LayerMapTryGet((uid, sprite), GunVisualLayers.MagUnshaded, out _, false))
            _sprite.LayerSetVisible((uid, sprite), GunVisualLayers.MagUnshaded, showUnshaded);
    }
}
