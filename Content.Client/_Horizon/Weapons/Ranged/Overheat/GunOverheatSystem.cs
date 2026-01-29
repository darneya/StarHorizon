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
        var heat = overheat.CurrentHeat;
        var color = Color.InterpolateBetween(Color.White, Color.Red, heat);
        _sprite.SetColor((uid, sprite), color);
    }
}
