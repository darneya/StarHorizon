using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Horizon.GhostSprites;

/// <summary>
/// Prototype for a ghost sprite that players can select.
/// </summary>
[Prototype("ghostSprite")]
public sealed partial class GhostSpritePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Display name for the sprite (localization key).
    /// </summary>
    [DataField("name", required: true)]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Path to the RSI file relative to Textures folder.
    /// </summary>
    [DataField("rsiPath", required: true)]
    public ResPath RsiPath { get; private set; } = default!;

    /// <summary>
    /// RSI state to use.
    /// </summary>
    [DataField("state")]
    public string State { get; private set; } = "animated";

    /// <summary>
    /// If true, only sponsors can select this sprite.
    /// </summary>
    [DataField("sponsorOnly")]
    public bool SponsorOnly { get; private set; } = false;
}
