using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Procedural;

[RegisterComponent]
public sealed partial class DungeonSpawnComponent : Component
{
    /// <summary>
    /// Указывает какой пресет использовать для генерации данжа
    /// </summary>
    [DataField("dungeonPreset", required: true)]
    public string DungeonPreset { get; set; } = string.Empty;

    /// <summary>
    /// Генерация структур по сидам, если пусто сид выбирается рамдомно
    /// </summary>
    [DataField("seed")]
    public int? Seed { get; set; }

}