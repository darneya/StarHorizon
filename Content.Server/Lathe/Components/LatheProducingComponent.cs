using Content.Shared.Research.Prototypes; // Horizon
                                          
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
    /// Horizon. Флаг, что указывает включено ли бесконечное производство. 
    /// </summary>
    [DataField]
    public bool InfiniteProduction;

    /// <summary>
    /// Horizon. Последний рецепт.
    /// </summary>
    [DataField]
    public LatheRecipePrototype? LastRecipe; 
}

