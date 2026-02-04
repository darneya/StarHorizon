using System.Linq;
using Content.Server._Horizon.Medical.Limbs;
using Content.Server.Body.Systems;
using Content.Shared._Horizon.Traits;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Server.Containers;
using Robust.Server.GameObjects;

namespace Content.Server._Horizon.Traits;

public sealed class QuirksSystem : SharedQuirksSystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly LimbSystem _limb = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TraitPendingBodyModificationComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var root = _body.GetRootPartOrNull(uid);
            if (root is null)
                return;

            for (var i = comp.Parts.Count - 1; i >= 0; i--)
            {
                var item = comp.Parts[i];
                if (ReplacePart(uid, (root.Value.Entity, root.Value.BodyPart), item.PartType, item.Symmetry, item.ProtoId, item.SlotId))
                    comp.Parts.Remove(item);
            }

            for (var i = comp.Organs.Count - 1; i >= 0; i--)
            {
                var item = comp.Organs[i];
                if (ReplaceOrgan(uid, (root.Value.Entity, root.Value.BodyPart), item.OrganSlot, item.OrganProto))
                    comp.Organs.Remove(item);
            }

            if (comp.Organs.Count <= 0 && comp.Parts.Count <= 0)
                RemCompDeferred(uid, comp);
        }
    }

    public bool ReplacePart(EntityUid uid, Entity<BodyPartComponent> root, BodyPartType removePartType, BodyPartSymmetry symmerty, string? protoId, string? slotId)
    {
        var parts = _body.GetBodyChildrenOfType(uid, removePartType);
        bool success = false;

        foreach (var part in parts)
        {
            var partComp = part.Component;
            if (partComp.Symmetry != symmerty)
                continue;

            if (!_body.TryGetParentBodyPart(part.Id, out var parent, out var parentComp))
                continue;

            foreach (var child in _body.GetBodyPartChildren(part.Id, part.Component))
            {
                _limb.TryAmputate(uid, child.Id);
                QueueDel(child.Id);
            }

            _limb.TryAmputate(uid, part.Id);
            QueueDel(part.Id);

            // apparently chopping off limbs makes people bleed a lot. Who would have guessed?
            _bloodstream.TryModifyBleedAmount(uid, -10f);

            if (protoId is null || slotId == null)
                continue;

            var newLimb = SpawnAtPosition(protoId, Transform(uid).Coordinates);
            if (TryComp<BodyPartComponent>(newLimb, out var limbComp) && limbComp.Symmetry == symmerty)
                _limb.TryAttachLimb(uid, slotId, (parent.Value, parentComp), (newLimb, limbComp));

            success = true;
        }

        return success;
    }

    public bool ReplaceOrgan(EntityUid uid, Entity<BodyPartComponent> root, string organId, string protoId)
    {
        var parts = _body.GetBodyChildren(uid);
        bool success = false;

        foreach (var part in parts)
        {
            if (!_container.TryGetContainer(part.Id, organId, out var container) || container.ContainedEntities.Count <= 0)
                continue;

            var organ = container.ContainedEntities.First();
            _body.RemoveOrgan(organ);

            var newOrgan = Spawn(protoId, Transform(uid).Coordinates);
            _body.AddOrganToFirstValidSlot(part.Id, newOrgan);

            success = true;
        }

        return success;
    }
}
