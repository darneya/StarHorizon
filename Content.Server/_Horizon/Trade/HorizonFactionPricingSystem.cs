using Content.Server.Station.Systems;
using Content.Shared._Horizon.Trade;
using Content.Shared.Cargo;

namespace Content.Server._Horizon.Trade;

/// <summary>
/// Handles faction-based pricing for trade goods.
/// Items with HorizonFactionPriceComponent have different prices depending on which faction's station they're sold at.
/// </summary>
public sealed class HorizonFactionPricingSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HorizonFactionPriceComponent, PriceCalculationEvent>(OnPriceCalculation);
    }

    private void OnPriceCalculation(Entity<HorizonFactionPriceComponent> ent, ref PriceCalculationEvent ev)
    {
        // Get the station this entity is on
        var owningStation = _station.GetOwningStation(ent);

        // Determine the faction of the station
        var faction = HorizonFaction.Market; // Default to Market if no faction component found

        if (owningStation != null && TryComp<HorizonStationFactionComponent>(owningStation, out var factionComp))
        {
            faction = factionComp.Faction;
        }

        // Set price based on faction
        ev.Price = faction switch
        {
            HorizonFaction.AnCo => ent.Comp.PriceAnCo,
            HorizonFaction.Dfi => ent.Comp.PriceDfi,
            HorizonFaction.Syndicate => ent.Comp.PriceSyndicate,
            HorizonFaction.Pirate => ent.Comp.PricePirate,
            HorizonFaction.NanoTraisen => ent.Comp.PriceNanoTraisen,
            _ => ent.Comp.PriceMarket
        };

        // Ensure non-negative price
        ev.Price = double.Max(0.0, ev.Price);

        // Mark as handled to prevent StaticPrice from overriding
        ev.Handled = true;
    }

    /// <summary>
    /// Gets the price of an entity for a specific faction.
    /// </summary>
    public double GetFactionPrice(EntityUid uid, HorizonFaction faction)
    {
        if (!TryComp<HorizonFactionPriceComponent>(uid, out var priceComp))
            return 0;

        return faction switch
        {
            HorizonFaction.AnCo => priceComp.PriceAnCo,
            HorizonFaction.Dfi => priceComp.PriceDfi,
            HorizonFaction.Syndicate => priceComp.PriceSyndicate,
            HorizonFaction.Pirate => priceComp.PricePirate,
            HorizonFaction.NanoTraisen => priceComp.PriceNanoTraisen,
            _ => priceComp.PriceMarket
        };
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
