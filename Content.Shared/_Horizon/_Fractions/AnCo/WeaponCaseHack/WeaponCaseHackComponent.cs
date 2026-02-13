using Robust.Shared.Serialization;

namespace Content.Shared._Horizon._Fractions.AnCo.WeaponCaseHack;

/// <summary>
/// Component for weapon cases that can be hacked via wires.
/// The correct wire is determined by the serial number.
/// Uses LockComponent for actual locking.
/// </summary>
[RegisterComponent]
public sealed partial class WeaponCaseHackComponent : Component
{
    /// <summary>
    /// Whether the correct wire has been cut.
    /// </summary>
    [DataField]
    public bool CutCompleted;

    /// <summary>
    /// Whether the correct wire has been pulsed.
    /// </summary>
    [DataField]
    public bool PulseCompleted;
}

[Serializable, NetSerializable]
public enum WeaponCaseHackWireActionKey : byte
{
    Status
}
