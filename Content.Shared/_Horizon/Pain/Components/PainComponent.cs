using System.Numerics;
using Content.Shared._Horizon.Pain.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Pain.Components;

/// <summary>
/// Отвечает за уровень боли игрока и его реакцию на вещи
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class PainComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public float CurrentPain = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField, AutoNetworkedField]
    public PainStages CurrentStage = PainStages.Nothing;

    [DataField]
    public SortedDictionary<PainStages, float> PainThresholds = new();

    [DataField("surgeryPrototypes")]
    public List<string> AllowedSurgeryProtypes = [];

    // Список визгов, криков, кашлей от боли
    [DataField("screamPrototype")]
    public string ScreamOfPainPrototype = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextPossibleScream = TimeSpan.Zero;

    // Конвертер единиц урона в единицы боли
    [DataField("converterPrototype")]
    public string DamagePrototypeConverter = string.Empty;

    [DataField]
    public TimeSpan PopupUpdateTime = TimeSpan.FromSeconds(15);

    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<string, byte> GunshotsCount = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 TotalDamage = FixedPoint2.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan EffectCooldown = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public Vector2 TotalImpulse = Vector2.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? EndGunshotsTime = TimeSpan.FromMilliseconds(500);
}
