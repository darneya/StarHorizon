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

namespace Content.Client._Horizon.Cytology.Swab;

public sealed class CytologySwabSystem : SharedCytologySwabSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CytologySwabComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<CytologySwabComponent> swab, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var swabSprite = (swab.Owner, args.Sprite);
        if (_sprite.LayerMapTryGet(swabSprite, CytologySwabVisualLayers.Sample, out var sampleLayer, false))
        {
            Appearance.TryGetData(swab.Owner, CytologySwabVisualStates.IsVisible, out bool isSampleVisible);
            _sprite.LayerSetVisible(swabSprite, sampleLayer, isSampleVisible);

            if (swab.Comp.TextureState != null)
            {
                _sprite.LayerSetRsiState(swabSprite, sampleLayer, swab.Comp.TextureState);
            }
        }
    }

}
