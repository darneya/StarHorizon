using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.OutpostCapture;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("outpostSpawn")]
public sealed partial class OutpostSpawnList : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("spawn")]
    public List<EntitySpawnEntry> SpawnList = new();
}
