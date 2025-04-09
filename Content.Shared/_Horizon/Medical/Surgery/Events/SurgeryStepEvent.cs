using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Medical.Surgery.Events;

[ByRefEvent]
public record struct SurgeryStepEvent(EntityUid User, EntityUid Body, EntityUid Part, List<EntityUid> Tools)
{
    public required EntProtoId StepProto { get; init; }
    public required EntProtoId SurgeryProto { get; init; }
    public bool IsCancelled { get; set; }
}
[ByRefEvent]
public record struct SurgeryStepCompleteEvent(EntityUid User, EntityUid Body, EntityUid Part, List<EntityUid> Tools)
{
    public required EntProtoId StepProto { get; init; }
    public required EntProtoId SurgeryProto { get; init; }
    public required bool IsFinal { get; init; }
}
