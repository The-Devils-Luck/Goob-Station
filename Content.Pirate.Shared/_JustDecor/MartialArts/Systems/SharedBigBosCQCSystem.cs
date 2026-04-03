using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Content.Goobstation.Common.Grab;
using Content.Goobstation.Common.MartialArts;
using Content.Goobstation.Shared.GrabIntent;
using Content.Goobstation.Shared.MartialArts.Components;
using Content.Pirate.Shared._JustDecor.MartialArts.Events;
using Content.Pirate.Shared._JustDecor.MartialArts.Components;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Systems;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Components;
using Content.Shared._Shitmed.Targeting;
using Content.Shared._White.Grab;
using Content.Shared.Weapons.Melee;
using Content.Goobstation.Shared.MartialArts;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chat;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Throwing;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Pirate.Shared._JustDecor.MartialArts;

public sealed class SharedBigBosCQCSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly Content.Shared.StatusEffectNew.StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly GrabThrownSystem _grabThrowing = default!;
    [Dependency] private readonly TraumaSystem _trauma = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly SleepingSystem _sleeping = default!;

    // Словник для зберігання кулдаунів
    private readonly Dictionary<EntityUid, Dictionary<string, TimeSpan>> _cooldowns = new();

    public override void Initialize()
    {
        base.Initialize();

        // Події для BigBos CQC combo
        SubscribeLocalEvent<CanPerformComboComponent, BigBosCqcTakedownPerformedEvent>(OnBigBosCQCTakedown);
        SubscribeLocalEvent<CanPerformComboComponent, BigBosCqcDisarmPerformedEvent>(OnBigBosCQCDisarm);
        SubscribeLocalEvent<CanPerformComboComponent, BigBosCqcThrowPerformedEvent>(OnBigBosCQCThrow);
        SubscribeLocalEvent<CanPerformComboComponent, BigBosCqcChokePerformedEvent>(OnBigBosCQCChoke);
        SubscribeLocalEvent<CanPerformComboComponent, BigBosCqcChainPerformedEvent>(OnBigBosCQCChain);
        SubscribeLocalEvent<CanPerformComboComponent, BigBosCqcCounterPerformedEvent>(OnBigBosCQCCounter);
        SubscribeLocalEvent<CanPerformComboComponent, BigBosCqcInterrogationPerformedEvent>(OnBigBosCQCInterrogation);
        SubscribeLocalEvent<CanPerformComboComponent, BigBosCqcStealthTakedownPerformedEvent>(OnBigBosCQCStealthTakedown);
        SubscribeLocalEvent<CanPerformComboComponent, BigBosCqcRushPerformedEvent>(OnBigBosCQCRush);
        SubscribeLocalEvent<CanPerformComboComponent, BigBosCqcFinisherPerformedEvent>(OnBigBosCQCFinisher);

        // DoAfter події
        SubscribeLocalEvent<BigBosCqcInterrogationDoAfterEvent>(OnInterrogationComplete);
        SubscribeLocalEvent<BigBosCqcChokeDoAfterEvent>(OnChokeComplete);

        // Додаткові підписки на події
        SubscribeLocalEvent<BigBosCqcRushBuffComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<BigBosCqcKnowledgeComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshCombatMovespeed);
        SubscribeLocalEvent<BigBosCqcCounterBuffComponent, DamageModifyEvent>(OnCounterDamageModify);
        SubscribeLocalEvent<BigBosCqcKnowledgeComponent, ComponentStartup>(OnKnowledgeStartup);

        // Пасивна абілка
        SubscribeLocalEvent<BigBosCqcKnowledgeComponent, MeleeHitEvent>(OnMeleeHit);

        // Події для отримання знання
        SubscribeLocalEvent<Components.GrantBigBosCQCComponent, MapInitEvent>(OnGrantMapInit);
        SubscribeLocalEvent<Components.GrantBigBosCQCComponent, ComponentStartup>(OnGrantStartup);
        SubscribeLocalEvent<Components.GrantBigBosCQCComponent, UseInHandEvent>(OnCqcItemUsed);

        // Події для очищення кулдаунів при видаленні сутності
        SubscribeLocalEvent<BigBosCqcKnowledgeComponent, ComponentShutdown>(OnKnowledgeShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateBuffs(frameTime);
        UpdateCombatModes(frameTime);
        UpdateChokeholds(frameTime);
    }

    // --- Основні методи комбо ---

    private void OnBigBosCQCTakedown(Entity<CanPerformComboComponent> ent, ref BigBosCqcTakedownPerformedEvent args)
    {
        if (!TryGetTarget(ent, out var target, out var proto) || IsDown(target))
            return;

        if (!CheckCooldown(ent, "Takedown"))
            return;

        // МИТТЄВИЙ нокдаун з шкодою
        DoDamage(ent, target, "Blunt", proto.ExtraDamage);
        _stun.TryKnockdown(target, TimeSpan.FromSeconds(proto.ParalyzeTime), true);

        var staminaDamage = new DamageSpecifier();
        staminaDamage.DamageDict.Add("Stamina", 40f);
        _damageable.TryChangeDamage(target, staminaDamage, origin: ent);

        // Всі предмети з рук цілі падають, разом з ціллю
        DropAllItems(target);
        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, "CQC Takedown");
        SetCooldown(ent, "Takedown", TimeSpan.FromSeconds(1.5));
        ClearLastAttacks(ent);
    }

    private void OnBigBosCQCDisarm(Entity<CanPerformComboComponent> ent, ref BigBosCqcDisarmPerformedEvent args)
    {
        if (!TryGetTarget(ent, out var target, out var proto))
            return;

        if (!CheckCooldown(ent, "Disarm"))
            return;

        // Краде речі з рук цілі
        StealAllItems(ent.Owner, target);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg"), target);
        ComboPopup(ent, target, "Disarming Strike");
        SetCooldown(ent, "Disarm", TimeSpan.FromSeconds(1));
        ClearLastAttacks(ent);
    }

    private void OnBigBosCQCThrow(Entity<CanPerformComboComponent> ent, ref BigBosCqcThrowPerformedEvent args)
    {
        if (!TryGetTarget(ent, out var target, out var proto))
            return;

        if (!CheckCooldown(ent, "Throw"))
            return;

        var mapPos = _transform.GetMapCoordinates(ent).Position;
        var hitPos = _transform.GetMapCoordinates(target).Position;
        var dir = (hitPos - mapPos).Normalized();

        DoDamage(ent, target, "Blunt", proto.ExtraDamage);
        _stun.TryKnockdown(target, TimeSpan.FromSeconds(proto.ParalyzeTime), true);
        _grabThrowing.Throw(target, ent, dir, 8f);

        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit1.ogg"), target);
        ComboPopup(ent, target, "Over-Shoulder Throw");
        SetCooldown(ent, "Throw", TimeSpan.FromSeconds(1));
        ClearLastAttacks(ent);
    }

    private void OnBigBosCQCChoke(Entity<CanPerformComboComponent> ent, ref BigBosCqcChokePerformedEvent args)
    {
        if (!TryGetTarget(ent, out var target, out var proto))
            return;

        if (!CheckCooldown(ent, "Choke"))
            return;

        // Чокхолд івент
        var doAfterArgs = new DoAfterArgs(EntityManager, ent, TimeSpan.FromSeconds(3), new BigBosCqcChokeDoAfterEvent(), ent, target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        ComboPopup(ent, target, "Chokehold Started");
        SetCooldown(ent, "Choke", TimeSpan.FromSeconds(2));
        ClearLastAttacks(ent);
    }

    private void OnChokeComplete(BigBosCqcChokeDoAfterEvent args)
    {
        if (args.Cancelled || args.Args.Target == null)
            return;

        var ent = args.Args.User;
        var target = args.Args.Target.Value;

        var staminaDamage = new DamageSpecifier();
        staminaDamage.DamageDict.Add("Stamina", 100f);
        _damageable.TryChangeDamage(target, staminaDamage, origin: ent);

        // Force sleep like a stealth takedown
        if (_netManager.IsServer)
        {
            _stun.TryKnockdown(target, TimeSpan.FromSeconds(15), true);
            _sleeping.TrySleeping(target);
            _statusEffects.TryAddStatusEffectDuration(target, "StatusEffectForcedSleeping", out _, TimeSpan.FromSeconds(8));
        }

        ComboPopup(ent, target, "Chokehold Complete");
    }

    private void OnBigBosCQCChain(Entity<CanPerformComboComponent> ent, ref BigBosCqcChainPerformedEvent args)
    {
        if (!TryGetTarget(ent, out var target, out var proto))
            return;

        if (!CheckCooldown(ent, "Chain"))
            return;

        // Серія багаторазових ударів
        DoDamage(ent, target, "Blunt", proto.ExtraDamage);

        var staminaDamage = new DamageSpecifier();
        staminaDamage.DamageDict.Add("Stamina", proto.StaminaDamage);
        _damageable.TryChangeDamage(target, staminaDamage, origin: ent);

        // Декілька звукових ефектів для серії
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit1.ogg"), target);

        // Затримка другого удару
        Timer.Spawn(TimeSpan.FromMilliseconds(300), () =>
        {
            if (TerminatingOrDeleted(target) || TerminatingOrDeleted(ent))
                return;

            DoDamage(ent, target, "Blunt", proto.ExtraDamage / 2);
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit2.ogg"), target);
        });

        ComboPopup(ent, target, "Chain Attack");
        SetCooldown(ent, "Chain", TimeSpan.FromSeconds(1.5));
        ClearLastAttacks(ent);
    }

    private void OnBigBosCQCCounter(Entity<CanPerformComboComponent> ent, ref BigBosCqcCounterPerformedEvent args)
    {
        if (!TryGetTarget(ent, out var target, out var proto))
            return;

        if (!CheckCooldown(ent, "Counter"))
            return;

        // Контратака з ефектом оглушення
        DoDamage(ent, target, "Blunt", 20f);
        _stun.TryKnockdown(target, TimeSpan.FromSeconds(8), true);

        var staminaDamage = new DamageSpecifier();
        staminaDamage.DamageDict.Add("Stamina", 60f);
        _damageable.TryChangeDamage(target, staminaDamage, origin: ent);

        // Коротка невразливість для користувача
        var counterBuff = EnsureComp<BigBosCqcCounterBuffComponent>(ent);
        counterBuff.EndTime = _timing.CurTime + TimeSpan.FromSeconds(2);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, "Counter Attack");
        SetCooldown(ent, "Counter", TimeSpan.FromSeconds(2));
        ClearLastAttacks(ent);
    }

    private void OnBigBosCQCInterrogation(Entity<CanPerformComboComponent> ent, ref BigBosCqcInterrogationPerformedEvent args)
    {
        if (!TryGetTarget(ent, out var target, out var proto))
            return;

        if (!TryComp<NpcInterrogatableComponent>(target, out var interrogatable) || interrogatable.Prototype == null)
        {
            if (_netManager.IsServer)
                _popup.PopupEntity("Ціль не піддається інтерогації.", ent, ent);
            return;
        }

        if (!CheckCooldown(ent, "Interrogation"))
            return;

        // Скидання стану
        interrogatable.PendingLinkedAnswers = null;
        var questionToSay = "Відповідай!";

        if (_proto.TryIndex(interrogatable.Prototype.Value, out var interrogationProto))
        {
            // Створення зваженого списку опцій:
            // 0 = General Question (weight based on count)
            // 1 = Linked Question (weight based on count)

            var options = new List<(string Question, List<string>? specificAnswers)>();

            // Додаємо всі загальні запитання (якщо є)
            foreach (var q in interrogationProto.Questions)
            {
                options.Add((q, null));
            }

            // Додаємо всі пов'язані запитання (якщо є)
            foreach (var linked in interrogationProto.Linked)
            {
                if (!string.IsNullOrWhiteSpace(linked.Question))
                {
                    options.Add((linked.Question, linked.Answers));
                }
            }

            // Вибираємо одне випадково, якщо є опції
            if (options.Count > 0)
            {
                var selected = _random.Pick(options);
                questionToSay = selected.Question;
                interrogatable.PendingLinkedAnswers = selected.specificAnswers;
            }
        }

        TrySay(ent, questionToSay);

        // Start interrogation DoAfter
        var doAfterArgs = new DoAfterArgs(EntityManager, ent, TimeSpan.FromSeconds(3), new BigBosCqcInterrogationDoAfterEvent(), ent, target)
        {
            BreakOnMove = true,
            BreakOnDamage = false,
            NeedHand = false
        };

        // Починаємо "елімінувати" їх негайно, щоб вони не чинили опір під час процесу
        EnsureComp<Content.Shared.CombatMode.Pacification.PacifiedComponent>(target);

        _doAfter.TryStartDoAfter(doAfterArgs);
        ComboPopup(ent, target, "Interrogation Started");
        SetCooldown(ent, "Interrogation", TimeSpan.FromSeconds(2));
        ClearLastAttacks(ent);
    }

    private void OnInterrogationComplete(BigBosCqcInterrogationDoAfterEvent args)
    {
        if (args.Cancelled || args.Args.Target == null)
            return;

        var ent = args.Args.User;
        var target = args.Args.Target.Value;

        // Ефекти допиту - розкриває інформацію, накладає страх
        var staminaDamage = new DamageSpecifier();
        staminaDamage.DamageDict.Add("Stamina", 40f);
        _damageable.TryChangeDamage(target, staminaDamage, origin: ent);

        // Логіка допиту NPC
        if (TryComp<NpcInterrogatableComponent>(target, out var interrogatable))
        {
            string? answer = null;

            if (interrogatable.Prototype != null && _proto.TryIndex(interrogatable.Prototype.Value, out var interrogationProto))
            {
                var potentialAnswers = interrogatable.PendingLinkedAnswers ?? interrogationProto.Answers;
                if (potentialAnswers.Count > 0)
                {
                    answer = _random.Pick(potentialAnswers);
                }
            }

            // Резервна відповідь за замовчуванням
            if (string.IsNullOrEmpty(answer))
            {
                answer = "Відпусти!";
            }

            if (_netManager.IsServer)
            {
                Timer.Spawn(TimeSpan.FromMilliseconds(400), () =>
                {
                    if (TerminatingOrDeleted(target))
                        return;
                    TrySay(target, answer);
                });
            }

            interrogatable.Interrogated = true;
            interrogatable.PendingLinkedAnswers = null;

            // Елімінувати NPC - зробити пацифікованим та зупинити ШІ
            // NpcEliminatedComponent відновить ШІ через певний час
            EnsureComp<NpcEliminatedComponent>(target);
            EnsureComp<Content.Shared.CombatMode.Pacification.PacifiedComponent>(target);

            _popup.PopupEntity("Ціль елімінована та не становитиме загрози.", ent, ent);
        }

        ComboPopup(ent, target, "Interrogation Complete");
    }

    private void OnBigBosCQCStealthTakedown(Entity<CanPerformComboComponent> ent, ref BigBosCqcStealthTakedownPerformedEvent args)
    {
        if (!TryGetTarget(ent, out var target, out var proto))
            return;

        if (!CheckCooldown(ent, "StealthTakedown"))
            return;

        // Стелс-повалення працює тільки коли ціль НЕ в бойовому режимі
        if (TryComp<CombatModeComponent>(target, out var combatMode) && combatMode.IsInCombatMode)
        {
            _popup.PopupEntity("Ціль у бойовому режимі! Стелс атака неможлива.", ent, ent);
            return;
        }

        // Тихе повалення зі спини
        DoDamage(ent, target, "Blunt", 25f);
        _stun.TryKnockdown(target, TimeSpan.FromSeconds(12), true);
        _statusEffects.TryAddStatusEffectDuration(target, "StatusEffectForcedSleeping", out _, TimeSpan.FromSeconds(8));

        // Без звуку для стелсу
        ComboPopup(ent, target, "Stealth Takedown");
        SetCooldown(ent, "StealthTakedown", TimeSpan.FromSeconds(2));
        ClearLastAttacks(ent);
    }

    private void OnBigBosCQCRush(Entity<CanPerformComboComponent> ent, ref BigBosCqcRushPerformedEvent args)
    {
        if (!TryGetTarget(ent, out var target, out var proto))
            return;

        if (!CheckCooldown(ent, "Rush"))
            return;

        // Штурмова атака з переміщенням
        var mapPos = _transform.GetMapCoordinates(ent).Position;
        var hitPos = _transform.GetMapCoordinates(target).Position;
        var dir = (hitPos - mapPos).Normalized();

        DoDamage(ent, target, "Blunt", proto.ExtraDamage);

        var staminaDamage = new DamageSpecifier();
        staminaDamage.DamageDict.Add("Stamina", proto.StaminaDamage);
        _damageable.TryChangeDamage(target, staminaDamage, origin: ent);
        _grabThrowing.Throw(target, ent, dir, 5f);

        // Додаємо баф швидкості руху користувачу
        var rushBuff = EnsureComp<BigBosCqcRushBuffComponent>(ent);
        rushBuff.EndTime = _timing.CurTime + TimeSpan.FromSeconds(3);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit1.ogg"), target);
        ComboPopup(ent, target, "CQC Rush");
        SetCooldown(ent, "Rush", TimeSpan.FromSeconds(1));
        ClearLastAttacks(ent);
    }

    private void OnBigBosCQCFinisher(Entity<CanPerformComboComponent> ent, ref BigBosCqcFinisherPerformedEvent args)
    {
        if (!TryGetTarget(ent, out var target, out var proto))
            return;

        if (!CheckCooldown(ent, "Finisher"))
            return;

        // Ультимативний добиваючий прийом - працює тільки на важко поранених цілях
        if (TryComp<DamageableComponent>(target, out var damageable))
        {
            damageable.Damage.DamageDict.TryGetValue("Stamina", out var staminaDamageValue);
            var staminaDamageAmount = staminaDamageValue;
            if (staminaDamageAmount > 80f) // Критичний поріг стаміни
            {
                // Нищівна шкода
                DoDamage(ent, target, "Blunt", 40f);
                _stun.TryKnockdown(target, TimeSpan.FromSeconds(20), true);
                _statusEffects.TryAddStatusEffectDuration(target, "StatusEffectForcedSleeping", out _, TimeSpan.FromSeconds(15));

                // Потенційний перелом шиї, якщо умови виконані
                if (TryComp(ent, out PullerComponent? puller) && puller.Pulling == target &&
                    TryComp(ent, out GrabIntentComponent? grabIntent) &&
                    TryComp(target, out PullableComponent? pullable) &&
                    TryComp(target, out BodyComponent? body) &&
                    grabIntent.GrabStage == GrabStage.Suffocate &&
                    TryComp(ent, out TargetingComponent? targeting) &&
                    targeting.Target == TargetBodyPart.Head &&
                    _mobThreshold.TryGetDeadThreshold(target, out var damageToKill))
                {
                    _pulling.TryStopPull(target, pullable);
                    var blunt = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Blunt"), damageToKill.Value);
                    _damageable.TryChangeDamage(target, blunt, true, targetPart: TargetBodyPart.Head);

                    ComboPopup(ent, target, "FINISHING MOVE");
                }
                else
                {
                    ComboPopup(ent, target, "Devastator");
                }
            }
        }
        else
        {
            // Звичайна сильна атака, якщо ціль не в критичному стані
            DoDamage(ent, target, "Blunt", proto.ExtraDamage);
            _stun.TryKnockdown(target, TimeSpan.FromSeconds(8), true);
            ComboPopup(ent, target, "Heavy Strike");
        }

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        SetCooldown(ent, "Finisher", TimeSpan.FromSeconds(20));
        ClearLastAttacks(ent);
    }

    // --- Обробники подій для предметів, бафів та модифікаторів ---

    private void OnGrantStartup(EntityUid uid, Components.GrantBigBosCQCComponent component, ComponentStartup args)
    {
        GrantBigBosCQC(uid);
    }

    private void OnGrantMapInit(EntityUid uid, Components.GrantBigBosCQCComponent component, MapInitEvent args)
    {
        GrantBigBosCQC(uid);
    }

    private void OnCqcItemUsed(EntityUid uid, Components.GrantBigBosCQCComponent comp, UseInHandEvent args)
    {
        var user = args.User;

        // Перевірка, чи вже має користувач знання CQC
        if (HasComp<BigBosCqcKnowledgeComponent>(user))
        {
            _popup.PopupEntity("Ви вже володієте навичками CQC!", user, user);
            return;
        }

        GrantBigBosCQC(user);

        _popup.PopupEntity(Loc.GetString("bigbos-cqc-knowledge-gained"), user, user);

        // Видалення предмета після використання
        QueueDel(uid);
    }

    private void GrantBigBosCQC(EntityUid user)
    {
        if (HasComp<BigBosCqcKnowledgeComponent>(user))
            return;

        EnsureComp<BigBosCqcKnowledgeComponent>(user);
        var canPerformCombo = EnsureComp<CanPerformComboComponent>(user);
        EnsureComp<MartialArtsKnowledgeComponent>(user);
        EnsureComp<PullerComponent>(user);
        EnsureComp<MeleeWeaponComponent>(user);

        if (_proto.TryIndex<MartialArtPrototype>("BigBosCloseQuartersCombat", out var martialArtsPrototype))
        {
            if (_proto.TryIndex(martialArtsPrototype.RoundstartCombos, out var comboListPrototype))
            {
                canPerformCombo.AllowedCombos.Clear();
                foreach (var item in comboListPrototype.Combos)
                {
                    canPerformCombo.AllowedCombos.Add(_proto.Index(item));
                }
            }

            if (TryComp<MartialArtsKnowledgeComponent>(user, out var knowledge))
            {
                knowledge.MartialArtsForm = MartialArtsForms.BigBosCloseQuartersCombat;
            }
        }

        Dirty(user, canPerformCombo);
    }

    private void OnRefreshMovespeed(EntityUid uid, BigBosCqcRushBuffComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        if (_timing.CurTime < comp.EndTime)
        {
            args.ModifySpeed(1.5f, 1.5f); // 50% збільшення швидкості
        }
    }

    private void OnRefreshCombatMovespeed(EntityUid uid, BigBosCqcKnowledgeComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        // Невелике збільшення швидкості в бойовому режимі - ONLY if in combat mode
        if (TryComp<CombatModeComponent>(uid, out var combat) && combat.IsInCombatMode)
        {
            args.ModifySpeed(1.15f, 1.15f); // 15% збільшення швидкості
        }
    }

    private void OnCounterDamageModify(EntityUid uid, BigBosCqcCounterBuffComponent comp, DamageModifyEvent args)
    {
        if (_timing.CurTime < comp.EndTime)
        {
            // Зменшення отримуваної шкоди на 75% під час Counter бафу
            args.Damage *= 0.25f;
        }
    }

    private void OnKnowledgeStartup(EntityUid uid, BigBosCqcKnowledgeComponent comp, ComponentStartup args)
    {
        // Ініціалізація компоненту знань
        if (!_cooldowns.ContainsKey(uid))
        {
            _cooldowns[uid] = new Dictionary<string, TimeSpan>();
        }
    }

    private void OnKnowledgeShutdown(EntityUid uid, BigBosCqcKnowledgeComponent comp, ComponentShutdown args)
    {
        // Очищення кулдаунів при видаленні компоненту
        _cooldowns.Remove(uid);
    }

    // --- Методи оновлення ---

    private void UpdateBuffs(float frameTime)
    {
        var query = EntityQueryEnumerator<BigBosCqcRushBuffComponent>();
        while (query.MoveNext(out var uid, out var rushBuff))
        {
            if (_timing.CurTime >= rushBuff.EndTime)
            {
                RemComp<BigBosCqcRushBuffComponent>(uid);
                _movementSpeed.RefreshMovementSpeedModifiers(uid);
            }
        }

        var counterQuery = EntityQueryEnumerator<BigBosCqcCounterBuffComponent>();
        while (counterQuery.MoveNext(out var uid, out var counterBuff))
        {
            if (_timing.CurTime >= counterBuff.EndTime)
            {
                RemComp<BigBosCqcCounterBuffComponent>(uid);
            }
        }
    }

    private void UpdateCombatModes(float frameTime)
    {
        // Оновлення бойових режимів та стану
        var query = EntityQueryEnumerator<BigBosCqcKnowledgeComponent, CombatModeComponent>();
        while (query.MoveNext(out var uid, out var knowledge, out var combat))
        {
            UpdateCombatState(uid, frameTime);

            // Haste decay logic
            if (knowledge.HasteMeter > 0)
            {
                knowledge.HasteMeter = Math.Max(0, knowledge.HasteMeter - knowledge.HasteDecayRate * frameTime);
                UpdateAttackSpeed(uid, knowledge);
            }

            // Movement haste increase
            if (TryComp<PhysicsComponent>(uid, out var physics) && physics.LinearVelocity.Length() > 0.1f)
            {
                AddHaste(uid, knowledge, 0.05f * frameTime);
            }
        }
    }

    private void UpdateAttackSpeed(EntityUid uid, BigBosCqcKnowledgeComponent knowledge)
    {
        if (!TryComp<MeleeWeaponComponent>(uid, out var melee))
            return;

        if (knowledge.OriginalAttackRate == null)
            knowledge.OriginalAttackRate = melee.AttackRate;

        var bonus = knowledge.HasteMeter * knowledge.MaxHasteBonus;
        melee.AttackRate = knowledge.OriginalAttackRate.Value * (1.0f + bonus);
        Dirty(uid, melee);
    }

    private void AddHaste(EntityUid uid, BigBosCqcKnowledgeComponent knowledge, float amount)
    {
        knowledge.HasteMeter = Math.Min(1.0f, knowledge.HasteMeter + amount);
        UpdateAttackSpeed(uid, knowledge);
    }

    private void UpdateChokeholds(float frameTime)
    {
        // Оновлення активних удушень
        // Цей метод може бути розширений для обробки специфічної логіки удушень
    }

    // --- Допоміжні методи ---

    private bool CheckCooldown(EntityUid uid, string ability)
    {
        if (!_cooldowns.TryGetValue(uid, out var cooldownDict))
            return true;

        if (!cooldownDict.TryGetValue(ability, out var cooldownEnd))
            return true;

        return _timing.CurTime >= cooldownEnd;
    }

    private void SetCooldown(EntityUid uid, string ability, TimeSpan duration)
    {
        if (!_cooldowns.TryGetValue(uid, out var cooldownDict))
        {
            cooldownDict = new Dictionary<string, TimeSpan>();
            _cooldowns[uid] = cooldownDict;
        }

        cooldownDict[ability] = _timing.CurTime + duration;
    }

    private void UpdateCombatState(EntityUid uid, float frameTime)
    {
        // Combo state reset is handled in SharedBigBosCQCSystem.Update
    }

    private void DropAllItems(EntityUid uid)
    {
        // Викинути предмети з усіх рук
        foreach (var handName in _hands.EnumerateHands(uid))
        {
            _hands.TryDrop(uid, handName);
        }
    }

    private void StealAllItems(EntityUid user, EntityUid target)
    {
        foreach (var handName in _hands.EnumerateHands(target))
        {
            var item = _hands.GetHeldItem(target, handName);
            if (item == null)
                continue;

            if (_hands.TryDrop(target, handName))
            {
                // Спробувати підібрати будь-якою вільною рукою, інакше залишиться на підлозі
                _hands.TryPickupAnyHand(user, item.Value);
            }
        }
    }

    private bool TryGetTarget(Entity<CanPerformComboComponent> ent, out EntityUid target, [NotNullWhen(true)] out ComboPrototype? proto)
    {
        target = EntityUid.Invalid;
        proto = null;

        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out proto) ||
            ent.Comp.CurrentTarget == null)
            return false;

        target = ent.Comp.CurrentTarget.Value;
        return true;
    }

    private void DoDamage(EntityUid user, EntityUid target, string damageType, float amount)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict.Add(damageType, amount);
        _damageable.TryChangeDamage(target, damage, origin: user);
    }

    private void OnMeleeHit(EntityUid uid, BigBosCqcKnowledgeComponent knowledge, MeleeHitEvent args)
    {
        // 1. Логіка прискорення
        if (args.IsHit)
        {
            AddHaste(uid, knowledge, 0.25f); // 25% прискорення за удар
        }

        // 2. Логіка Running Tackle (Збивання з ніг на бігу)
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        // Перевіряємо, чи користувач дійсно рухається з високою швидкістю
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        // Вимагає значної швидкості (4+ м/с) для активації
        if (physics.LinearVelocity.Length() < 4.0f)
            return;

        // Перевірка кулдауну
        if (!CheckCooldown(uid, "RunningTackle"))
            return;

        var target = args.HitEntities[0];

        if (IsDown(target))
            return;

        // Running tackle: збивання з ніг + кидок
        _stun.TryKnockdown(target, TimeSpan.FromSeconds(3), true);

        // Кинути ціль у напрямку руху
        if (args.Direction != null)
        {
            var direction = args.Direction.Value.Normalized();
            _throwing.TryThrow(target, direction * 4, 15f); // 4 units distance
        }

        // Додати бонусну шкоду до running tackle
        args.BonusDamage.DamageDict["Blunt"] = args.BonusDamage.DamageDict.GetValueOrDefault("Blunt") + 10f;

        _popup.PopupEntity("BigBos виконує Running Tackle!", uid, uid);
        _popup.PopupEntity("Вас збивають з ніг!", target, target);

        SetCooldown(uid, "RunningTackle", TimeSpan.FromSeconds(5));
    }

    private void TrySay(EntityUid speaker, string message)
    {
        if (!_netManager.IsServer || string.IsNullOrWhiteSpace(message))
            return;

        _chat.TrySendInGameICMessage(speaker, message, InGameICChatType.Speak,
            hideChat: false, hideLog: true, checkRadioPrefix: false);
    }

    private void ComboPopup(EntityUid user, EntityUid target, string comboName)
    {
        if (!_netManager.IsServer)
            return;

        _popup.PopupEntity($"BigBos executes: {comboName}!", user, user);
        _popup.PopupEntity($"BigBos uses {comboName} on you!", target, target);
    }

    private void ClearLastAttacks(Entity<CanPerformComboComponent> ent)
    {
        ent.Comp.LastAttacks.Clear();
    }

    private bool IsDown(EntityUid uid)
    {
        return _standingState.IsDown(uid);
    }
}
