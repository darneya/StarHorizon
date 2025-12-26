using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.OutpostCapture;

[Prototype("outpostSpawn")]
public sealed partial class OutpostSpawnList : IPrototype
{
    [IdDataField]
    public string ID { get; } = null!;

    [DataField("spawn")]
    public List<EntitySpawnEntry> SpawnList = [];
}
