using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Toggleable;

namespace Content.Shared._Horizon.Language;

public abstract partial class SharedLanguageSystem
{
    private void InitializeTranslator()
    {
        SubscribeLocalEvent<HandheldTranslatorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<HandheldTranslatorComponent, GetLanguagesEvent>(OnTranslatorGetLanguages);
    }

    private void OnTranslatorGetLanguages(EntityUid uid, HandheldTranslatorComponent comp, ref GetLanguagesEvent args)
    {
        if (!comp.Enabled)
            return;

        if (!TryComp<LanguageSpeakerComponent>(comp.User, out var speaker))
            return;

        if (speaker.Languages.Keys.Where(x => comp.Languages.ContainsKey(x)).Count() <= 0)
            return;

        foreach (var (key, value) in comp.Languages)
        {
            if (args.Translator.TryGetValue(key, out var currentKnowledge) && currentKnowledge < value)
                args.Translator[key] = value;
            else
                args.Translator.Add(key, value);
        }
    }

    private void OnExamined(EntityUid uid, HandheldTranslatorComponent component, ExaminedEvent args)
    {
        var state = Loc.GetString(component.Enabled
            ? "translator-enabled"
            : "translator-disabled");

        args.PushMarkup(state);
    }
}
