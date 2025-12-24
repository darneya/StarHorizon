namespace Content.Shared._Horizon.OutpostCapture;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class FactionComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string FactionName { get; set; }
}
