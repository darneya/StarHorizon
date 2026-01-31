using System.Linq;
using Content.Client._Horizon.Traits;
using Content.Client.Message;
using Content.Shared.Traits;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private QuirkCategory _selectedQuirkCategory = QuirkCategory.Positive;
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
            .OrderBy(q => Loc.GetString(q.Name));
        foreach (var quirk in quirks)
        {
            bool skip = false;

            foreach (var item in quirk.Requirments)
            {
                if (!item.CanApply(Profile, _entManager))
                    skip = true;
            }

            if (skip)
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
            bool canApply = count + (hasTrait ? -quirk.Cost : quirk.Cost) <= 0;
            var quirkButton = new QuirkEntry(quirk.Name, quirk.Description ?? "", cost, category, hasTrait, canApply)
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

    public enum QuirkCategory
    {
        Positive,
        Negative,
        Neutral
    }
}
