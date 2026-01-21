using Content.Shared._Horizon.FlavorText;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Horizon.FactionAccess;

/// <summary>
/// Component that restricts access to entities based on character faction.
/// Similar to AccessReaderComponent but checks faction instead of access levels.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FactionAccessComponent : Component
{
    /// <summary>
    /// Whether or not the faction access check is enabled.
    /// If not, it will always let people through.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// List of factions that are allowed to access this entity.
    /// If empty and Enabled is true, no one can access.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<CharacterFactionPrototype>> AllowedFactions = new();

    /// <summary>
    /// List of factions that are explicitly denied access, even if in AllowedFactions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<CharacterFactionPrototype>> DeniedFactions = new();

    /// <summary>
    /// Popup message shown when access is denied.
    /// </summary>
    [DataField]
    public LocId? DeniedMessage = "faction-access-denied";
}
