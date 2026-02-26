using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Trade;

/// <summary>
/// Defines different prices for an entity based on the faction of the station where it's sold.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HorizonFactionPriceComponent : Component
{
    /// <summary>
    /// Price when sold at Market stations (default).
    /// </summary>
    [DataField]
    public int PriceMarket;

    /// <summary>
    /// Price when sold at ANCO stations.
    /// </summary>
    [DataField]
    public int PriceAnco;

    /// <summary>
    /// Price when sold at DFI stations.
    /// </summary>
    [DataField]
    public int PriceDfi;

    /// <summary>
    /// Price when sold at NanoTraisen stations.
    /// </summary>
    [DataField]
    public int PriceNanoTraisen;

    /// <summary>
    /// Price when sold at Pirate stations.
    /// </summary>
    [DataField]
    public int PricePirate;

    /// <summary>
    /// Price when sold at Syndicate stations.
    /// </summary>
    [DataField]
    public int PriceSyndicate;
}
