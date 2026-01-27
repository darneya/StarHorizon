using Robust.Shared.Audio;

namespace Content.Server._Horizon.Implants.Components;

[RegisterComponent]
public sealed partial class ExtractTelepadComponent : Component
{
    /// <summary>
    /// Звук телепорта
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");
}
