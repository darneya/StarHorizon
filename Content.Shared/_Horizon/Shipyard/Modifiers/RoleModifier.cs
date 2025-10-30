using Content.Shared._Horizon.FlavorText;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;

namespace Content.Shared._Horizon.Shipyard;

public sealed partial class RoleModifier : BaseVesselCostModifier
{
    [DataField("role", required: true)]
    private string _role = string.Empty;

    public override void Modify(EntityUid? user, ref int cost, IEntityManager entMan)
    {
        if (user is not { Valid: true } uid)
            return;

        var mindSys = entMan.System<SharedMindSystem>();
        var jobSys = entMan.System<SharedJobSystem>();

        if (!mindSys.TryGetMind(uid, out var mindId, out var mind) || !jobSys.MindTryGetJob(mindId, out var job))
            return;

        if (job.ID != _role)
            return;

        cost = (int)(cost * CostMultiplier);
    }
}
