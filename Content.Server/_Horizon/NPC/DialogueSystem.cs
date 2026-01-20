using Content.Shared._Horizon.NPC;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Content.Server.Chat.Systems;
using Content.Server.Chat.Managers;
using System.Linq;
using Content.Server.Popups;
using Content.Shared.Mobs.Systems;

namespace Content.Server._Horizon.NPC
{
    public sealed class DialogueSystem : EntitySystem
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DialogueComponent, GetVerbsEvent<InteractionVerb>>(AddDialogueVerbs);
        SubscribeLocalEvent<DialogueStateComponent, GetVerbsEvent<InteractionVerb>>(AddStateVerbs);
    }

    private void AddDialogueVerbs(EntityUid uid, DialogueComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<DialogueStateComponent>(uid, out var state))
            return;

        switch (state.State)
        {
            case DialogueState.Idle:
                // Добавляем вкладку "Поговорить"
                var talkVerb = new InteractionVerb
                {
                    Act = () => StartDialogue(uid, args.User, component),
                    Text = "Поговорить",
                    Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/sentient.svg.192dpi.png")),
                    Priority = 2
                };
                args.Verbs.Add(talkVerb);
                break;

            case DialogueState.Talking when state.CurrentResponse == "follow":
                // Добавляем вкладку "Ответить" с подменю
                AddResponseVerbs(uid, args.User, args);
                break;

            case DialogueState.Following:
                // Добавляем вкладку "Отказать в сопровождении"
                var stopFollowVerb = new InteractionVerb
                {
                    Act = () => StopFollow(uid, args.User),
                    Text = "Отказать в сопровождении",
                    Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/close.svg.192dpi.png")),
                    Priority = 3
                };
                args.Verbs.Add(stopFollowVerb);
                break;
        }
    }

    private void AddResponseVerbs(EntityUid npc, EntityUid user, GetVerbsEvent<InteractionVerb> args)
    {
        var replyCategory = new VerbCategory("Ответить", "/Textures/Interface/VerbIcons/information.svg.192dpi.png");

        var acceptVerb = new InteractionVerb
        {
            Act = () => AcceptFollow(npc, user),
            Text = "Согласиться",
            Category = replyCategory,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/plus.svg.192dpi.png")),
            Priority = 1
        };
        args.Verbs.Add(acceptVerb);

        var declineVerb = new InteractionVerb
        {
            Act = () => DeclineFollow(npc, user),
            Text = "Отказать",
            Category = replyCategory,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/smite.svg.192dpi.png")),
            Priority = 2
        };
        args.Verbs.Add(declineVerb);
    }

    public void StartDialogue(EntityUid npc, EntityUid user, DialogueComponent? component = null)
    {
        if (!Resolve(npc, ref component))
        return;

        // Проверяем, жив ли моб
        if (_mobState.IsDead(npc) ||
        EntityManager.IsQueuedForDeletion(npc))
        return;

        var state = EnsureComp<DialogueStateComponent>(npc);
        state.State = DialogueState.Talking;

        if (_prototypeManager.TryIndex<DialogueTreePrototype>(component.DialogueTree, out var dialogueTree))
        {
            var dialogue = dialogueTree.Dialogues.FirstOrDefault(d => d.Id == "start");
            if (dialogue != null && dialogue.Responses.Count > 0)
            {
                // Сохраняем первый доступный ответ как текущий
                state.CurrentResponse = dialogue.Responses[0].Action;

                _chatSystem.TrySendInGameICMessage(
                    npc,
                    dialogue.Text,
                    InGameICChatType.Speak,
                    false
                );
            }
        }
    }

    private void AcceptFollow(EntityUid npc, EntityUid user)
    {
        if (_mobState.IsDead(npc) ||
        EntityManager.IsQueuedForDeletion(npc))
        return;

        var state = Comp<DialogueStateComponent>(npc);
        state.State = DialogueState.Following;
        state.CurrentResponse = null;

        _chatSystem.TrySendInGameICMessage(
        npc,
        "Отлично! Выведите меня отсюда поскорее.",
        InGameICChatType.Speak,
        false
    );

    var follow = EnsureComp<FollowComponent>(npc);
    follow.Target = user;
    }

    private void DeclineFollow(EntityUid npc, EntityUid user)
    {
        var state = Comp<DialogueStateComponent>(npc);
        state.State = DialogueState.Idle;
        state.CurrentResponse = null;

        _chatSystem.TrySendInGameICMessage(
            npc,
            "Я предложил вам 15000! Это что действительно так мало для вас?!",
            InGameICChatType.Speak,
            false
        );
    }

    private void StopFollow(EntityUid npc, EntityUid user)
    {
        var state = Comp<DialogueStateComponent>(npc);
        state.State = DialogueState.Idle;

        RemComp<FollowComponent>(npc);

        _chatSystem.TrySendInGameICMessage(
            npc,
            "Хмф! Тоже мне.. ''Наёмник''.",
            InGameICChatType.Speak,
            false
        );
    }

    private void AddStateVerbs(EntityUid uid, DialogueStateComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        // Этот метод будет вызван автоматически для всех сущностей с DialogueStateComponent
        // Здесь можно добавить дополнительную логику если нужно
    }
}
}
