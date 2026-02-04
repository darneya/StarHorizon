using Content.Shared._Horizon.Cryptominer;
using JetBrains.Annotations;

namespace Content.Client._Horizon.Cryptominer;

[UsedImplicitly]
public sealed class CryptominerBoundUserInterface : BoundUserInterface
{
    private CryptominerMenu? _window;

    public CryptominerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new CryptominerMenu(this);
        _window.OnClose += Close;
        _window?.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Dispose();
        _window = null;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CryptominerBoundUserInterfaceState current)
            return;

        _window?.UpdateState(current);
    }

    public void SendToggle()
    {
        SendMessage(new CryptominerToggleMessage());
    }
}
