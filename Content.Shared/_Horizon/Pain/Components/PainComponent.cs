using Content.Shared.Damage;

namespace Content.Shared._Horizon.Pain.Components;

/// <summary>
/// Отвечает за уровень боли игрока и его реакцию на вещи
/// </summary>
[RegisterComponent]
public sealed partial class PainComponent : Component
{
    [DataField]
    public SortedDictionary<PainStages, float> PainThresholds = new();

    [DataField]
    public Dictionary<string, DamageSpecifier> AllowedSurgery = [];

    /// <summary>
    /// Список визгов, криков, кашлей от боли
    /// </summary>
    [DataField]
    public List<string> PainScreamList = [];

    [DataField]
    public Dictionary<string, float> DamageToPainConv = new();

    /// <summary>
    /// Конвертация единиц урона в время адреналина
    /// </summary>
    [DataField]
    public (TimeSpan, float) AdrenalinRushTime = (TimeSpan.FromSeconds(1), 5f);

    [DataField]
    public float ReducePainPerUpdate = 1f;

    /// <summary>
    /// Время в течении которого игрок не чувствует последствий урона
    /// нанесённого ему от пуль, порезов, ожогов и т.д.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? AdrenalinRush;

    [ViewVariables(VVAccess.ReadOnly)]
    public float CurrentPain = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    public PainStages CurrentStage = PainStages.Nothing;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan UpdateTime = TimeSpan.FromSeconds(5);

}
