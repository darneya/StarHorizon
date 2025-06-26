using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.Weapons.Ranged.Upgrades.Components;

[NetworkedComponent, Access(typeof(GunUpgradeSystem))]
public sealed partial class GunUpgradeComponent : Component
{
    [DataField]
    public List<ProtoId<TagPrototype>> Tags = new();

    [DataField]
    public LocId ExamineText;
}
