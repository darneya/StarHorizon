namespace Content.Shared._Horizon.Pain.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class PainComponent : Component
{
    [DataField]
    public Dictionary<PainStages, sbyte> PainThresholds = new();

    /// <summary>
    /// Список визгов, криков, кашлей от боли
    /// </summary>
    [DataField]
    public List<string> PainScreamList = [];
}
