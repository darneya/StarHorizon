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
    /// Указывает, включено ли бесконечное производство.
    /// </summary>
    [DataField]
    public bool InfiniteProduction = false;

    [DataField]
    public LatheRecipePrototype? LastRecipe = null;
}

