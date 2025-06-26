using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Weapons.Ranged.Upgrades.Components;

[NetworkedComponent, Access(typeof(GunUpgradeSystem))]
public sealed partial class GunComponentUpgrateComponent : Component
{
    [DataField]
    public ComponentRegistry Components = new();
}
