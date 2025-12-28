using System.Linq;
using Content.Client.UserInterface.Fragments;
using Content.Shared._Horizon.Expeditions;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._Horizon.Expeditions.Ui;

/// <summary>
///     UI fragment responsible for displaying NanoTask controls in a PDA and coordinating with the NanoTaskCartridgeSystem for state
/// </summary>
public sealed partial class GoalsListUi : UIFragment
{
    private GoalsListUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new();
        _fragment.RemovePressed += args => userInterface.SendMessage(new CartridgeUiMessage(new GoalsListRemoveMessage(args)));
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not GoalsListCartridgeUiState cast)
            return;

        _fragment?.Populate(cast.Goals, IoCManager.Resolve<IEntityManager>());
    }
}
