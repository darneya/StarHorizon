namespace Content.Shared.Aavikko.Medical.Surgery.Events;

[ByRefEvent]
public record struct SurgeryValidEvent(EntityUid Body, EntityUid Part, bool Cancelled = false, string Suffix = "");
