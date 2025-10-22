using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;
using Content.Shared.Chemistry;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Content.Shared.Chemistry.Reagent;
using Content.Server.Materials.Components;

namespace Content.Shared._Horizon.Cytology.Systems;

public sealed class SharedMicroscope
{
    public const string InputSlotName = "petriDishSlot";
}

[Serializable, NetSerializable]
public sealed class CellSampleInfo // TODO думать что делать с дубликатом
{

    public readonly string DisplayName;

    public readonly List<ProtoId<ReagentPrototype>> RequiredChemicals;

    public readonly List<ProtoId<ReagentPrototype>> SupplementaryChemicals;

    public readonly List<ProtoId<ReagentPrototype>> SuppressiveChemicals;

    public readonly float GrowthRateInSeconds;

    public readonly float ViralSusceptibility;

    public CellSampleInfo(string displayName,
                          List<ProtoId<ReagentPrototype>> requiredChemicals,
                          List<ProtoId<ReagentPrototype>> supplementaryChemicals,
                          List<ProtoId<ReagentPrototype>> suppressiveChemicals,
                          float growthRateInSeconds, float viralSusceptibility)
    {
        DisplayName = displayName;
        RequiredChemicals = requiredChemicals;
        SupplementaryChemicals = supplementaryChemicals;
        SuppressiveChemicals = suppressiveChemicals;
        GrowthRateInSeconds = growthRateInSeconds;
        ViralSusceptibility = viralSusceptibility;
    }
}

[Serializable, NetSerializable]
public sealed class MicroscopeBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<CellSampleInfo>? CellSampleInfo;

    public MicroscopeBoundUserInterfaceState(List<CellSampleInfo>? cellSampleInfo)
    {
        CellSampleInfo = cellSampleInfo;
    }
}

[Serializable, NetSerializable]
public enum MicroscopeUiKey
{
    Key
}
