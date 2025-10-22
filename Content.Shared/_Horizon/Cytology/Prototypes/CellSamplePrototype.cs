using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Content.Shared.Chemistry.Reagent;
using Content.Server.Materials.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Horizon.Cytology.Prototypes;

[Serializable, NetSerializable, DataDefinition]
[Prototype("cellSample")]
public sealed partial class CellSamplePrototype : IPrototype, ICloneable // TODO дать суммари
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField] public string Name = "cell";

    [DataField] public float GrowthRateInSeconds = 1f;

    [DataField] public float ViralSusceptibility = 1f;

    [DataField] public List<ProtoId<ReagentPrototype>> RequiredChemicals = new(); //TODO возможно, стоит заменить прото в string

    [DataField] public Dictionary<ProtoId<ReagentPrototype>, float> SupplementaryChemicals = new();

    [DataField] public Dictionary<ProtoId<ReagentPrototype>, float> SuppressiveChemicals = new();

    //TODO добавить сурс


    [DataField] public HashSet<string>? SpawnMobByPrototype;

    public object Clone()
    {
        return new CellSamplePrototype()
        {
            ID = ID,
            Name = Name,
            GrowthRateInSeconds = GrowthRateInSeconds,
            ViralSusceptibility = ViralSusceptibility,
            RequiredChemicals = RequiredChemicals,
            SupplementaryChemicals = SupplementaryChemicals,
            SuppressiveChemicals = SuppressiveChemicals,
            SpawnMobByPrototype = SpawnMobByPrototype
        };
    }
}

