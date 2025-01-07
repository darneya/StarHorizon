using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Aavikko.Medical.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class SurgeryClampBleedEffectComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class SurgeryStepAttachLimbEffectComponent : Component;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryStepBleedEffectComponent : Component
{
    [DataField]
    public DamageSpecifier? Damage;
}

[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class SurgeryStepAmputationEffectComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class SurgeryRemoveAccentComponent : Component;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSurgerySystem))] public sealed partial class SurgeryClearProgressComponent : Component;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryStepEmoteEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> Emote = "Scream";
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryStepSpawnEffectComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Entity;
}
