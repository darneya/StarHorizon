using System.Linq;
using Content.Client._Horizon.Traits;
using Content.Client.Message;
using Content.Shared.Traits;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private QuirkCategory _selectedQuirkCategory = QuirkCategory.Positive;
    private List<TraitPrototype> _cachedQuirks = new();
    private const string QuirksCategory = "HorizonQuirks";

    private void StartupQuirks()
    {
        TraitsTab.SetTabTitle(0, Loc.GetString("humanoid-profile-editor-quirks-tab"));
        TraitsTab.SetTabTitle(1, Loc.GetString("humanoid-profile-editor-minor-traits-tab"));

        ButtonGroup categoryGroup = new(false);

        TraitsPositive.Group = categoryGroup;
        TraitsNegative.Group = categoryGroup;
        TraitsNeutral.Group = categoryGroup;

        TraitsPositive.Pressed = true;

        TraitsPositive.OnPressed += _ =>
        {
            _selectedQuirkCategory = QuirkCategory.Positive;
            RefreshQuirks();
        };
        TraitsNegative.OnPressed += _ =>
        {
            _selectedQuirkCategory = QuirkCategory.Negative;
            RefreshQuirks();
        };
        TraitsNeutral.OnPressed += _ =>
        {
            _selectedQuirkCategory = QuirkCategory.Neutral;
            RefreshQuirks();
        };
    }

    private void RefreshQuirks()
    {
        QuirksList.Children.Clear();

        if (Profile is null)
            return;

        var count = 0;
        foreach (var trait in Profile.TraitPreferences)
        {
            // If trait not found or another category don't count its points.
            if (!_prototypeManager.TryIndex<TraitPrototype>(trait, out var otherProto) ||
                otherProto.Category != QuirksCategory)
            {
                continue;
            }

            count += otherProto.Cost;
        }

        QuirksPointsLabel.SetMarkup(Loc.GetString("humanoid-profile-editor-quirks-points-label", ("points", -count)));

        var quirks = _prototypeManager
            .EnumeratePrototypes<TraitPrototype>()
            .Where(q => q.Category == QuirksCategory)
            .OrderBy(q => Loc.GetString(q.Name))
            .ToList();

        if (_cachedQuirks.Equals(quirks))
        {
            foreach (var item in QuirksList.Children)
            {
                if (item is not QuirkEntry entry)
                    continue;

                var quirk = _prototypeManager.Index<TraitPrototype>(entry.ProtoId);

                bool hasTrait = Profile.TraitPreferences.Contains(quirk.ID);
                bool canApply = count + (hasTrait ? -quirk.Cost : quirk.Cost) <= 0;

                entry.UpdateEntry(hasTrait, canApply);
            }
        }
        else
        {
            _cachedQuirks = quirks;

            foreach (var quirk in quirks)
            {
                if (!quirk.RequirmentsMet(Profile, _entManager))
                {
                    Profile = Profile.WithoutTraitPreference(quirk.ID, _prototypeManager);

                    SetDirty();
                    UpdateSaveButton();
                    continue;
                }

                var cost = -quirk.Cost;
                var category = cost switch
                {
                    > 0 => QuirkCategory.Negative,
                    < 0 => QuirkCategory.Positive,
                    _ => QuirkCategory.Neutral
                };

                if (category != _selectedQuirkCategory)
                    continue;

                bool hasTrait = Profile.TraitPreferences.Contains(quirk.ID);

                var quirkButton = new QuirkEntry(quirk.ID, quirk.Name, quirk.Description ?? "", cost, category, hasTrait, CanApplyQuirk(quirk, count))
                {
                    Margin = new Thickness(0, 2)
                };
                quirkButton.OnTraitToggled += isSelected =>
                {
                    Profile = isSelected ? Profile.WithTraitPreference(quirk.ID, _prototypeManager) : Profile.WithoutTraitPreference(quirk.ID, _prototypeManager);

                    SetDirty();
                    RefreshQuirks();
                    UpdateSaveButton();
                };
                QuirksList.AddChild(quirkButton);
            }
        }
    }

    private string? CanApplyQuirk(TraitPrototype trait, int points)
    {
        if (Profile == null)
            return null;

        string? reason = null;

        bool canApply = points + (Profile.TraitPreferences.Contains(trait.ID) ? -trait.Cost : trait.Cost) <= 0;

        if (!canApply)
        {
            reason = Profile.TraitPreferences.Contains(trait.ID) ? Loc.GetString("humanoid-profile-editor-quirks-cannot-remove") :
                                                                   Loc.GetString("humanoid-profile-editor-quirks-cannot-add");
        }

        if (trait.Group != null && !Profile.TraitPreferences.Contains(trait.ID))
        {
            foreach (var item in Profile.TraitPreferences)
            {
                var proto = _prototypeManager.Index(item);
                if (proto.Group == null)
                    continue;

                if (proto.Group == trait.Group)
                    reason = Loc.GetString($"humanoid-profile-editor-quirks-cannot-add-group-{proto.Group}");
            }
        }

        return reason;
    }

    public enum QuirkCategory
    {
        Positive,
        Negative,
        Neutral
    }
}
