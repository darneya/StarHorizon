namespace Content.Shared._Horizon.Pain.Prototypes;

[ByRefEvent]
public record struct PainEffectEvent(string Key, bool Cancelled = false, string Reason = "");

public enum PainStages : byte
{
    Nothing = 0,
    MildPain = 1,
    AveragePain = 2,
    SeverePain = 3,
    UnbeatablePain = 4,
}
