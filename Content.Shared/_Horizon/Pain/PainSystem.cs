namespace Content.Shared._Horizon.Pain;

/// <summary>
///
/// </summary>
public sealed class PainSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {

    }
}

public enum PainStages : byte
{
    MildPain = 0,
    AveragePain = 1,
    SeverePain = 2,
    UnbeatablePain = 3,
}
