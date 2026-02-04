using Content.Shared._Horizon.FlavorText;
using Content.Shared.Access;
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

    /// <summary>
    /// Whether this entity can be unlocked/locked using an ID card with required access.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanToggleLock = true;

    /// <summary>
    /// Access level required to unlock/lock this entity.
    /// If null, uses AllowedFactions check instead.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<AccessLevelPrototype>? UnlockAccess = "AnCo";

    /// <summary>
    /// Whether this entity is currently unlocked (accessible to everyone).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Unlocked;
}
