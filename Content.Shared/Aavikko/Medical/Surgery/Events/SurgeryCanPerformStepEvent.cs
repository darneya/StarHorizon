using Content.Shared.Inventory;

namespace Content.Shared.Aavikko.Medical.Surgery.Events;

[ByRefEvent]
public record struct SurgeryCanPerformStepEvent(
    EntityUid User,
    EntityUid Body,
    List<EntityUid> Tools,
    SlotFlags TargetSlots,
    string? Popup = null,
    StepInvalidReason Invalid = StepInvalidReason.None
) : IInventoryRelayEvent
{
    public HashSet<EntityUid> ValidTools = [];
}
