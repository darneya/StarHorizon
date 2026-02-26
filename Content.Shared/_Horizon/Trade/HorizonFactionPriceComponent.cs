using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Trade;

/// <summary>
/// Компонент для хранения цен торговых товаров в зависимости от фракции.
/// PriceMarket — базовая цена, остальные значения — множители от неё.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HorizonFactionPriceComponent : Component
{
    /// <summary>
    /// Базовая цена товара (для Market).
    /// </summary>
    [DataField]
    public double PriceMarket = 5000;

    /// <summary>
    /// Множитель цены для фракции AnCo.
    /// Итоговая цена = PriceMarket × PriceAnCo.
    /// </summary>
    [DataField]
    public double PriceAnCo = 1.0;

    /// <summary>
    /// Множитель цены для фракции DFI.
    /// Итоговая цена = PriceMarket × PriceDfi.
    /// </summary>
    [DataField]
    public double PriceDfi = 1.0;

    /// <summary>
    /// Множитель цены для фракции NanoTraisen.
    /// Итоговая цена = PriceMarket × PriceNanoTraisen.
    /// </summary>
    [DataField]
    public double PriceNanoTraisen = 1.0;

    /// <summary>
    /// Множитель цены для фракции Pirate.
    /// Итоговая цена = PriceMarket × PricePirate.
    /// </summary>
    [DataField]
    public double PricePirate = 1.0;

    /// <summary>
    /// Множитель цены для фракции Syndicate.
    /// Итоговая цена = PriceMarket × PriceSyndicate.
    /// </summary>
    [DataField]
    public double PriceSyndicate = 1.0;
}
