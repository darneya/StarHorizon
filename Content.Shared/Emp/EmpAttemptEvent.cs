using Content.Shared.Emp;

namespace Content.Shared.Emp;

/// <summary>
/// Raised on an entity before <see cref="EmpPulseEvent"/>.
/// Cancel this to prevent the emp event being raised.
/// </summary>
public sealed partial class EmpAttemptEvent : CancellableEntityEventArgs
{
    public bool Cancelled { get; set; }
}
