using Robust.Shared.GameStates;

namespace Content.Shared._Horizon.Trade;

/// <summary>
/// Компонент для хранения множителей цены торговых товаров в зависимости от фракции.
/// Цена рассчитывается как: (стоимость ресурсов × 5) × множитель_фракции
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HorizonFactionPriceComponent : Component
{
    /// <summary>
    /// Базовая цена для расчета множителей (Market).
    /// Используется как основа для расчета множителей других фракций.
    /// </summary>
    [DataField]
    public double PriceMarket = 1.0;

    /// <summary>
    /// Множитель цены для фракции AnCo.
    /// Если 0, используется значение PriceMarket.
    /// </summary>
    [DataField]
    public double PriceAnCo = 1.0;

    /// <summary>
    /// Множитель цены для фракции DFI.
    /// Если 0, используется значение PriceMarket.
    /// </summary>
    [DataField]
    public double PriceDfi = 1.0;

    /// <summary>
    /// Множитель цены для фракции NanoTraisen.
    /// Если 0, используется значение PriceMarket.
    /// </summary>
    [DataField]
    public double PriceNanoTraisen = 1.0;

    /// <summary>
    /// Множитель цены для фракции Pirate.
    /// Если 0, используется значение PriceMarket.
    /// </summary>
    [DataField]
    public double PricePirate = 1.0;

    /// <summary>
    /// Множитель цены для фракции Syndicate.
    /// Если 0, используется значение PriceMarket.
    /// </summary>
    [DataField]
    public double PriceSyndicate = 1.0;
}
