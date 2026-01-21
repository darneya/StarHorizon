using Content.Shared._Horizon.FlavorText;
using Content.Shared.Popups;
using Content.Shared.UserInterface;

namespace Content.Shared._Horizon.FactionAccess;

/// <summary>
/// System that handles faction-based access checks.
/// Blocks ActivatableUI opening if the user doesn't belong to an allowed faction.
/// </summary>
public sealed class FactionAccessSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionAccessComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
    }

    private void OnUIOpenAttempt(Entity<FactionAccessComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!IsAllowed(args.User, ent))
        {
            args.Cancel();
            if (ent.Comp.DeniedMessage != null)
                _popup.PopupClient(Loc.GetString(ent.Comp.DeniedMessage), ent, args.User);
        }
    }

    /// <summary>
    /// Checks if a user is allowed to access an entity with FactionAccessComponent.
    /// </summary>
    public bool IsAllowed(EntityUid user, Entity<FactionAccessComponent> target)
    {
        if (!target.Comp.Enabled)
            return true;

        if (!TryComp<CharacterFactionMemberComponent>(user, out var factionMember))
            return false;

        var userFaction = factionMember.Faction;

        if (target.Comp.DeniedFactions.Contains(userFaction))
            return false;

        if (target.Comp.AllowedFactions.Count == 0)
            return false;

        return target.Comp.AllowedFactions.Contains(userFaction);
    }

    /// <summary>
    /// Checks if a user is allowed to access an entity with FactionAccessComponent.
    /// </summary>
    public bool IsAllowed(EntityUid user, EntityUid target)
    {
        if (!TryComp<FactionAccessComponent>(target, out var factionAccess))
            return true;

        return IsAllowed(user, (target, factionAccess));
    }
}
