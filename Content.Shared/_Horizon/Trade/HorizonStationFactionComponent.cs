using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.Trade;

/// <summary>
/// Defines which faction a station belongs to for trade pricing purposes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HorizonStationFactionComponent : Component
{
    /// <summary>
    /// The faction this station belongs to.
    /// </summary>
    [DataField(required: true)]
    public HorizonFaction Faction = HorizonFaction.Market;
}

/// <summary>
/// Factions for location-based trade pricing.
/// </summary>
[Serializable, NetSerializable]
public enum HorizonFaction : byte
{
    /// <summary>
    /// Standard market stations (default pricing).
    /// </summary>
    Market,

    /// <summary>
    /// ANCO corporation stations.
    /// </summary>
    AnCo,

    /// <summary>
    /// DFI faction stations.
    /// </summary>
    Dfi,

    /// <summary>
    /// Syndicate faction stations.
    /// </summary>
    Syndicate,

    /// <summary>
    /// Pirate faction stations.
    /// </summary>
    Pirate,

    /// <summary>
    /// NanoTraisen faction stations.
    /// </summary>
    NanoTraisen
}
