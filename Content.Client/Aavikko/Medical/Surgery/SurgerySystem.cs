using Content.Shared.Aavikko.Medical.Surgery;

namespace Content.Client.Aavikko.Medical.Surgery;

public sealed class SurgerySystem : SharedSurgerySystem
{
    public event Action? OnRefresh;
    public override void Update(float frameTime)
    {
        DelayAccumulator += frameTime;
        if (DelayAccumulator > 1)
        {
            DelayAccumulator = 0;
            OnRefresh?.Invoke();
        }
    }
}
