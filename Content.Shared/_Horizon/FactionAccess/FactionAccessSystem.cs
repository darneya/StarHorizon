using Content.Shared._Horizon.FlavorText;
using Content.Shared.Access.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.UserInterface;

namespace Content.Shared._Horizon.FactionAccess;

/// <summary>
/// System that handles faction-based access checks.
/// Blocks ActivatableUI opening and equipment if the user doesn't belong to an allowed faction.
/// Can be unlocked/locked by faction members using an ID card.
/// </summary>
public sealed class FactionAccessSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionAccessComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<FactionAccessComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<FactionAccessComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<FactionAccessComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!ent.Comp.CanToggleLock)
            return;

        // Check if using an ID card or PDA with ID card
        if (!HasComp<IdCardComponent>(args.Used) && !HasComp<PdaComponent>(args.Used))
            return;

        // Check if ID card has required access
        if (!HasUnlockAccess(args.Used, ent))
            return;

        // Toggle lock state
        ent.Comp.Unlocked = !ent.Comp.Unlocked;
        Dirty(ent);

        var message = ent.Comp.Unlocked
            ? Loc.GetString("faction-access-unlocked")
            : Loc.GetString("faction-access-locked");
        _popup.PopupClient(message, ent, args.User);

        args.Handled = true;
    }

    /// <summary>
    /// Checks if the ID card (or PDA with ID card) has the required access to toggle lock.
    /// </summary>
    private bool HasUnlockAccess(EntityUid used, Entity<FactionAccessComponent> target)
    {
        if (target.Comp.UnlockAccess == null)
            return false;

        // Get the ID card entity (from PDA if needed)
        EntityUid? idCard = null;

        if (TryComp<IdCardComponent>(used, out _))
        {
            idCard = used;
        }
        else if (TryComp<PdaComponent>(used, out var pda) && pda.ContainedId != null)
        {
            idCard = pda.ContainedId.Value;
        }

        if (idCard == null)
            return false;

        // Check if ID card has the required access
        if (!TryComp<AccessComponent>(idCard, out var access))
            return false;

        return access.Tags.Contains(target.Comp.UnlockAccess.Value);
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

    private void OnEquipAttempt(Entity<FactionAccessComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!IsAllowed(args.Equipee, ent))
        {
            args.Cancel();
            if (ent.Comp.DeniedMessage != null)
                args.Reason = ent.Comp.DeniedMessage;
        }
    }

    /// <summary>
    /// Checks if a user is allowed to access an entity with FactionAccessComponent.
    /// </summary>
    public bool IsAllowed(EntityUid user, Entity<FactionAccessComponent> target)
    {
        if (!target.Comp.Enabled)
            return true;

        // If unlocked, everyone can access
        if (target.Comp.Unlocked)
            return true;

        // No restrictions if both lists are empty
        if (target.Comp.AllowedFactions.Count == 0 && target.Comp.DeniedFactions.Count == 0)
            return true;

        if (!TryComp<CharacterFactionMemberComponent>(user, out var factionMember))
        {
            // No faction - only allow if no AllowedFactions specified
            return target.Comp.AllowedFactions.Count == 0;
        }

        var userFaction = factionMember.Faction;

        if (target.Comp.DeniedFactions.Contains(userFaction))
            return false;

        if (target.Comp.AllowedFactions.Count == 0)
            return true;

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
