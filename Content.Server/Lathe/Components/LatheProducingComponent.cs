using Content.Shared.Research.Prototypes; // Ссылка, которая имеет определение LatheRecipePrototype
                                          // Нужно явно указать её, чтобы можно было использовать LatheRecipePrototype.
namespace Content.Server.Lathe.Components;

/// <summary>
/// For EntityQuery to keep track of which lathes are producing
/// </summary>
[RegisterComponent]
public sealed partial class LatheProducingComponent : Component
{
    /// <summary>
    /// The time at which production began
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StartTime;

    /// <summary>
    /// How long it takes to produce the recipe.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ProductionLength;

    /// <summary>
    /// Флаг, что указывает включено ли бесконечное производство.
    /// </summary>
    [DataField]
    public bool InfiniteProduction; // = false; У bool базовое значение уже false, искл - bool?, там базовое значение null.

    [DataField]
    public LatheRecipePrototype? LastRecipe; // = null; Базовое значение уже null лучше не указывать его дважды
}

