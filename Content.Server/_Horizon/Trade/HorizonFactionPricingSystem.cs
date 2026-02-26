using Content.Server.Station.Systems;
using Content.Shared._Horizon.Trade;
using Content.Shared.Cargo;

namespace Content.Server._Horizon.Trade;

/// <summary>
/// Система расчета цены торговых товаров на основе стоимости ресурсов.
/// Формула: цена = (стоимость ресурсов × 5) × множитель_фракции
/// </summary>
public sealed class HorizonFactionPricingSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HorizonFactionPriceComponent, PriceCalculationEvent>(OnPriceCalculation);
    }

    private void OnPriceCalculation(Entity<HorizonFactionPriceComponent> ent, ref PriceCalculationEvent ev)
    {
        // Если событие уже обработано, не делаем ничего
        if (ev.Handled)
            return;

        // Получаем стоимость ресурсов в ящике
        var resourcePrice = _pricing.GetPrice(ent.Owner, includeContents: true);

        // Базовая цена = стоимость ресурсов × 5
        var basePrice = resourcePrice * 5.0;

        // Получаем станцию, на которой продается ящик
        var owningStation = _station.GetOwningStation(ent);

        // Определяем фракцию станции
        var faction = HorizonFaction.Market; // По умолчанию Market

        if (owningStation != null && TryComp<HorizonStationFactionComponent>(owningStation, out var factionComp))
        {
            faction = factionComp.Faction;
        }

        // Применяем множитель фракции
        var factionMultiplier = GetFactionMultiplier(ent.Comp, faction);
        var finalPrice = basePrice * factionMultiplier;

        // Устанавливаем итоговую цену
        ev.Price = finalPrice;

        // Гарантируем неотрицательную цену
        ev.Price = double.Max(0.0, ev.Price);

        // Помечаем как обработанное
        ev.Handled = true;
    }

    /// <summary>
    /// Получить множитель цены для конкретной фракции.
    /// </summary>
    private double GetFactionMultiplier(HorizonFactionPriceComponent comp, HorizonFaction faction)
    {
        return faction switch
        {
            HorizonFaction.AnCo => comp.PriceAnCo > 0 ? comp.PriceAnCo / comp.PriceMarket : 1.0,
            HorizonFaction.Dfi => comp.PriceDfi > 0 ? comp.PriceDfi / comp.PriceMarket : 1.0,
            HorizonFaction.Syndicate => comp.PriceSyndicate > 0 ? comp.PriceSyndicate / comp.PriceMarket : 1.0,
            HorizonFaction.Pirate => comp.PricePirate > 0 ? comp.PricePirate / comp.PriceMarket : 1.0,
            HorizonFaction.NanoTraisen => comp.PriceNanoTraisen > 0 ? comp.PriceNanoTraisen / comp.PriceMarket : 1.0,
            _ => 1.0
        };
    }

    /// <summary>
    /// Gets the price of an entity for a specific faction.
    /// </summary>
    public double GetFactionPrice(EntityUid uid, HorizonFaction faction)
    {
        if (!TryComp<HorizonFactionPriceComponent>(uid, out var priceComp))
            return 0;

        var resourcePrice = _pricing.GetPrice(uid, includeContents: true);
        var basePrice = resourcePrice * 5.0;

        var multiplier = GetFactionMultiplier(priceComp, faction);
        return basePrice * multiplier;
    }

    /// <summary>
    /// Gets the faction of a station.
    /// </summary>
    public HorizonFaction GetStationFaction(EntityUid? station)
    {
        if (station == null)
            return HorizonFaction.Market;

        if (TryComp<HorizonStationFactionComponent>(station, out var factionComp))
            return factionComp.Faction;

        return HorizonFaction.Market;
    }
}
