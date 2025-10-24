namespace Content.Shared._Horizon.Pain.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class ThrowOnProjectileHitComponent : Component
{
    [DataField]
    public byte GunshotsToThrowBody = 6;

    [DataField]
    public TimeSpan ThrowDuration = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan GunshotsTime = TimeSpan.FromSeconds(0.5);

    [DataField]
    public float AdditionalForce = 15f;
}
