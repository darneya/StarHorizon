using Content.Shared.Emp;
using Content.Shared._rage.Emp;

namespace Content.Server.Emp;

public sealed class EmpImmuneSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpImmuneComponent, EmpAttemptEvent>(OnEmpAttempt);
    }

    private void OnEmpAttempt(Entity<EmpImmuneComponent> ent, ref EmpAttemptEvent args)
    {
        // Используем метод Cancel() вместо прямого присваивания
        args.Cancel();
    }
}
