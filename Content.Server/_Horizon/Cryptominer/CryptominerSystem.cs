using Content.Server.Atmos.EntitySystems;
using Content.Server.Cargo.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._Horizon.Cryptominer;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Horizon.Cryptominer;

public sealed class CryptominerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    private float _updateTimer;
    private const float UpdateTime = 1.0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryptominerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CryptominerComponent, CryptominerToggleMessage>(OnToggle);
        SubscribeLocalEvent<CryptominerComponent, EntInsertedIntoContainerMessage>(OnDiskInserted);
        SubscribeLocalEvent<CryptominerComponent, EntRemovedFromContainerMessage>(OnDiskRemoved);
    }

    private void OnDiskInserted(EntityUid uid, CryptominerComponent miner, EntInsertedIntoContainerMessage args)
    {
        UpdateDiskCount(uid, miner);
    }

    private void OnDiskRemoved(EntityUid uid, CryptominerComponent miner, EntRemovedFromContainerMessage args)
    {
        UpdateDiskCount(uid, miner);
    }

    private void UpdateDiskCount(EntityUid uid, CryptominerComponent miner)
    {
        var count = 0;

        if (_itemSlots.GetItemOrNull(uid, CryptominerComponent.DiskSlot1) != null)
            count++;
        if (_itemSlots.GetItemOrNull(uid, CryptominerComponent.DiskSlot2) != null)
            count++;
        if (_itemSlots.GetItemOrNull(uid, CryptominerComponent.DiskSlot3) != null)
            count++;
        if (_itemSlots.GetItemOrNull(uid, CryptominerComponent.DiskSlot4) != null)
            count++;

        miner.DiskCount = count;
        _appearance.SetData(uid, CryptominerVisuals.DiskCount, count);
        Dirty(uid, miner);
    }

    private void OnPowerChanged(EntityUid uid, CryptominerComponent miner, ref PowerChangedEvent args)
    {
        if (!args.Powered && miner.State != CryptominerState.Off)
        {
            miner.State = CryptominerState.Off;
            UpdateAppearance(uid, miner);
        }

        Dirty(uid, miner);
        UpdateUI(uid, miner);
    }

    private void OnToggle(EntityUid uid, CryptominerComponent miner, CryptominerToggleMessage args)
    {
        if (!_power.IsPowered(uid))
            return;

        if (miner.State == CryptominerState.Off || miner.State == CryptominerState.NoAtmosphere)
        {
            // Check atmosphere before turning on
            var environment = _atmosphere.GetContainingMixture(uid, true);
            var pressure = environment?.Pressure ?? 0f;

            if (pressure < miner.MinimumPressure)
            {
                miner.State = CryptominerState.NoAtmosphere;
            }
            else
            {
                miner.State = CryptominerState.Normal;
            }
        }
        else
        {
            miner.State = CryptominerState.Off;
        }

        Dirty(uid, miner);
        UpdateAppearance(uid, miner);
        UpdateUI(uid, miner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;

        if (_updateTimer < UpdateTime)
            return;

        _updateTimer -= UpdateTime;

        var query = EntityQueryEnumerator<CryptominerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var miner, out var xform))
        {
            var isPowered = _power.IsPowered(uid);

            // Get current atmosphere
            var environment = _atmosphere.GetContainingMixture(uid, true);
            if (environment != null)
            {
                miner.CurrentTemperature = environment.Temperature;
                miner.CurrentPressure = environment.Pressure;
            }
            else
            {
                miner.CurrentPressure = 0f;
            }

            // Check for low atmosphere - cannot operate in vacuum
            if (miner.CurrentPressure < miner.MinimumPressure && miner.State != CryptominerState.Off)
            {
                miner.State = CryptominerState.NoAtmosphere;
                miner.Efficiency = 0f;
                Dirty(uid, miner);
                UpdateAppearance(uid, miner);
                UpdateUI(uid, miner);
                continue;
            }

            // If not powered or off, skip processing
            if (!isPowered || miner.State == CryptominerState.Off)
            {
                if (miner.State != CryptominerState.Off && !isPowered)
                {
                    miner.State = CryptominerState.Off;
                    UpdateAppearance(uid, miner);
                }

                Dirty(uid, miner);
                UpdateUI(uid, miner);
                continue;
            }

            // Update state based on temperature
            miner.PreviousState = miner.State;
            UpdateTemperatureState(uid, miner);

            // Update vent state based on temperature transitions
            UpdateVentState(miner);

            // If critical, the miner shuts down
            if (miner.State == CryptominerState.Critical)
            {
                miner.Efficiency = 0f;
                Dirty(uid, miner);
                UpdateAppearance(uid, miner);
                UpdateUI(uid, miner);
                continue;
            }

            // Calculate efficiency based on temperature
            CalculateEfficiency(miner);

            // Add heat to the environment
            if (environment != null)
            {
                var heatToAdd = miner.HeatEnergyPerSecond * UpdateTime;
                _atmosphere.AddHeat(environment, heatToAdd);
            }

            // Generate credits
            var creditsGenerated = miner.CreditsPerSecond * miner.Efficiency * UpdateTime;
            miner.AccumulatedCredits += creditsGenerated;

            // Transfer whole credits to station bank
            if (miner.AccumulatedCredits >= 1.0f)
            {
                var wholeCredits = (int)miner.AccumulatedCredits;
                miner.AccumulatedCredits -= wholeCredits;
                miner.TotalCreditsEarned += wholeCredits;

                // Add credits to station bank
                AddCreditsToStation(uid, wholeCredits);
            }

            if (miner.PreviousState != miner.State)
            {
                UpdateAppearance(uid, miner);
            }

            Dirty(uid, miner);
            UpdateUI(uid, miner);
        }
    }

    private void UpdateTemperatureState(EntityUid uid, CryptominerComponent miner)
    {
        if (miner.CurrentTemperature >= miner.CriticalTemperature)
        {
            miner.State = CryptominerState.Critical;
        }
        else if (miner.CurrentTemperature >= miner.OverheatTemperature)
        {
            miner.State = CryptominerState.Overheat;
        }
        else if (miner.CurrentTemperature >= miner.WarningTemperature)
        {
            miner.State = CryptominerState.Warning;
        }
        else
        {
            miner.State = CryptominerState.Normal;
        }
    }

    private void CalculateEfficiency(CryptominerComponent miner)
    {
        if (miner.CurrentTemperature < miner.WarningTemperature)
        {
            miner.Efficiency = 1.0f;
        }
        else if (miner.CurrentTemperature < miner.OverheatTemperature)
        {
            // Linear interpolation from 100% to 75% efficiency
            var t = (miner.CurrentTemperature - miner.WarningTemperature) /
                    (miner.OverheatTemperature - miner.WarningTemperature);
            miner.Efficiency = 1.0f - (t * 0.25f);
        }
        else if (miner.CurrentTemperature < miner.CriticalTemperature)
        {
            // Linear interpolation from 75% to 25% efficiency
            var t = (miner.CurrentTemperature - miner.OverheatTemperature) /
                    (miner.CriticalTemperature - miner.OverheatTemperature);
            miner.Efficiency = 0.75f - (t * 0.5f);
        }
        else
        {
            miner.Efficiency = 0f;
        }
    }

    private void UpdateVentState(CryptominerComponent miner)
    {
        // Open vent when entering Overheat or Critical state
        if ((miner.State == CryptominerState.Overheat || miner.State == CryptominerState.Critical) &&
            miner.PreviousState != CryptominerState.Overheat &&
            miner.PreviousState != CryptominerState.Critical)
        {
            miner.IsVentOpen = true;
        }
        // Close vent when leaving Overheat/Critical state
        else if ((miner.PreviousState == CryptominerState.Overheat || miner.PreviousState == CryptominerState.Critical) &&
                 miner.State != CryptominerState.Overheat &&
                 miner.State != CryptominerState.Critical)
        {
            miner.IsVentOpen = false;
        }
    }

    private static readonly ProtoId<CargoAccountPrototype> CargoAccount = "Cargo";

    private void AddCreditsToStation(EntityUid minerUid, int amount)
    {
        var station = _station.GetOwningStation(minerUid);
        if (station == null)
            return;

        if (!TryComp<StationBankAccountComponent>(station, out var bank))
            return;

        _cargo.UpdateBankAccount((station.Value, bank), amount, CargoAccount);
    }

    private void UpdateAppearance(EntityUid uid, CryptominerComponent miner)
    {
        _appearance.SetData(uid, CryptominerVisuals.State, miner.State);
        _appearance.SetData(uid, CryptominerVisuals.IsVentOpen, miner.IsVentOpen);
    }

    private void UpdateUI(EntityUid uid, CryptominerComponent miner)
    {
        var isPowered = _power.IsPowered(uid);

        var state = new CryptominerBoundUserInterfaceState(
            miner.State,
            miner.CurrentTemperature,
            miner.WarningTemperature,
            miner.OverheatTemperature,
            miner.CriticalTemperature,
            miner.CreditsPerSecond,
            miner.TotalCreditsEarned,
            miner.Efficiency,
            miner.PowerConsumption,
            isPowered
        );

        _ui.SetUiState(uid, CryptominerUiKey.Key, state);
    }
}
