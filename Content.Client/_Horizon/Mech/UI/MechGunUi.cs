using Content.Client.UserInterface.Fragments;
using Content.Shared._Horizon.Mech;
using Robust.Client.UserInterface;

namespace Content.Client._Horizon.Mech.UI;

public sealed partial class MechGunUi : UIFragment
{
    private MechGunUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        if (fragmentOwner == null)
            return;

        _fragment = new MechGunUiFragment();

        _fragment.FragmentOwner = fragmentOwner;

        _fragment.ReloadAction += _ =>
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            userInterface.SendMessage(new MechGunReloadMessage(entManager.GetNetEntity(fragmentOwner.Value)));
            _fragment.StartTimer();
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MechGunUiState gunState)
            return;

        _fragment?.UpdateContents(gunState);
    }
}
