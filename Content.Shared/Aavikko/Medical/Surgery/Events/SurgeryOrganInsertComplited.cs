namespace Content.Shared.Aavikko.Medical.Surgery.Events;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
[ByRefEvent]
public record struct SurgeryOrganInsertCompleted(EntityUid Body, EntityUid Part, EntityUid Organ);
[ByRefEvent]
public record struct SurgeryOrganExtractCompleted(EntityUid Body, EntityUid Part, EntityUid Organ);
