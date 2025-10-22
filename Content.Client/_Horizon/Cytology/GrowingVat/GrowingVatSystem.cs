using Content.Client.Power;
using Content.Shared._Horizon.Cytology;
using Content.Shared._Horizon.Cytology.Components;
using Content.Shared._Horizon.Cytology.Prototypes;
using Content.Shared._Horizon.Cytology.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Lathe;
using Content.Shared.Power;
using Content.Shared.Research.Prototypes;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Runtime.CompilerServices;
using Dependency = Robust.Shared.IoC.DependencyAttribute;
using Content.Shared._Horizon.Cytology;
using Robust.Shared.Maths;
using Robust.Client.GameObjects;
using Content.Shared.Lathe;
using Content.Shared.Power;
using Content.Client.Power;
using Content.Shared.Research.Prototypes;

namespace Content.Client._Horizon.Cytology.GrowingVat;

public sealed class GrowingVatSystem : SharedGrowingVatSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CytologyGrowingVatComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<CytologyGrowingVatComponent> growingVat, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateIndicatorLayer(growingVat, args.Sprite, args.Component);
        UpdateLiquidLayer(growingVat, args.Sprite, args.Component);
    }

    private void UpdateLiquidLayer(Entity<CytologyGrowingVatComponent> growingVat, SpriteComponent sprite, AppearanceComponent appearance)
    {
        var growingVatSprite = (growingVat.Owner, sprite);
        if (_sprite.LayerMapTryGet(growingVatSprite, CytologyGrowingVatVisualLayers.Liquid, out var liquidLayer, false))
        {
            if (!TryGetSolutionFromBeaker(growingVat.Owner, out var beakerSolution) || beakerSolution.Volume <= 0)
            {
                _sprite.LayerSetVisible(growingVatSprite, liquidLayer, false);
                return;
            }

            _sprite.LayerSetVisible(growingVatSprite, liquidLayer, true);
            _sprite.LayerSetRsiState(growingVatSprite, liquidLayer, "soup");

            var averageColor = CalculateAverageReagentColor(beakerSolution);
            _sprite.LayerSetColor(growingVatSprite, liquidLayer, averageColor);

        }
    }

    private Color CalculateAverageReagentColor(Solution solution) // TODO Если оно где-то нужно будет, перенесем в шейред или посмотреть реализацию в солюшен
    {
        if (solution.Volume <= 0)
            return Color.White;

        var totalVolume = FixedPoint2.Zero;
        var colorSum = Vector3.Zero;

        foreach (var reagent in solution.Contents)
        {
            if (!_prototypeManager.TryIndex<ReagentPrototype>(reagent.Reagent.Prototype, out var reagentProto))
                continue;

            var volume = reagent.Quantity;
            totalVolume += volume;

            var reagentColor = reagentProto.SubstanceColor;
            var colorVector = new Vector3(reagentColor.R, reagentColor.G, reagentColor.B);
            colorSum += colorVector * (float)volume;
        }

        if (totalVolume <= 0)
            return Color.White;

        var averageColorVector = colorSum / (float)totalVolume;
        return new Color(
            Math.Clamp(averageColorVector.X, 0f, 1f),
            Math.Clamp(averageColorVector.Y, 0f, 1f),
            Math.Clamp(averageColorVector.Z, 0f, 1f),
            1f
        );
    }

    private void UpdateIndicatorLayer(Entity<CytologyGrowingVatComponent> growingVat, SpriteComponent sprite, AppearanceComponent appearance)
    {
        var growingVatSprite = (growingVat.Owner, sprite);

        if (_appearance.TryGetData<bool>(growingVat.Owner, PowerDeviceVisuals.Powered, out var powered, appearance) &&
            _sprite.LayerMapTryGet(growingVatSprite, PowerDeviceVisualLayers.Powered, out var powerLayer, false))
        {
            _sprite.LayerSetVisible(growingVatSprite, powerLayer, powered);

            if (!growingVat.Comp.IsActive)
            {
                _sprite.LayerSetRsiState(growingVatSprite, powerLayer, "white");
                return;
            }

            if (_sprite.LayerMapTryGet(growingVatSprite, CytologyGrowingVatVisualLayers.Indicator, out var indicatorLayer, false))
            {
                var state = growingVat.Comp.StopWithError ? "red" : "green";

                _sprite.LayerSetVisible(growingVatSprite, indicatorLayer, true);
                _sprite.LayerSetRsiState(growingVatSprite, indicatorLayer, state);
            }
        }
    }
}
