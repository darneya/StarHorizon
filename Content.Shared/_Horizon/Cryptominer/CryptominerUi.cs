using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Cryptominer;

[Serializable, NetSerializable]
public sealed class CryptominerBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly CryptominerState State;
    public readonly float CurrentTemperature;
    public readonly float WarningTemperature;
    public readonly float OverheatTemperature;
    public readonly float CriticalTemperature;
    public readonly int CreditsPerSecond;
    public readonly int TotalCreditsEarned;
    public readonly float Efficiency;
    public readonly float PowerConsumption;
    public readonly bool IsPowered;

    public CryptominerBoundUserInterfaceState(
        CryptominerState state,
        float currentTemperature,
        float warningTemperature,
        float overheatTemperature,
        float criticalTemperature,
        int creditsPerSecond,
        int totalCreditsEarned,
        float efficiency,
        float powerConsumption,
        bool isPowered)
    {
        State = state;
        CurrentTemperature = currentTemperature;
        WarningTemperature = warningTemperature;
        OverheatTemperature = overheatTemperature;
        CriticalTemperature = criticalTemperature;
        CreditsPerSecond = creditsPerSecond;
        TotalCreditsEarned = totalCreditsEarned;
        Efficiency = efficiency;
        PowerConsumption = powerConsumption;
        IsPowered = isPowered;
    }
}

[Serializable, NetSerializable]
public sealed class CryptominerToggleMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public enum CryptominerUiKey : byte
{
    Key
}
