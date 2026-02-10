using Content.Pirate.Shared._JustDecor.MartialArts.Components;
using Content.Goobstation.Shared.MartialArts.Components;
using Robust.Shared.Prototypes;

namespace Content.Pirate.Shared._JustDecor.MartialArts.Systems;

/// <summary>
/// Допоміжна система для інтеграції ComboHelper з різними martial arts системами.
/// Надає методи для управління ComboHelper компонентом з інших систем.
/// </summary>
public sealed class ComboHelperIntegrationSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    /// <summary>
    /// Додає ComboHelper компонент до entity та налаштовує його з заданим прототипом.
    /// </summary>
    public bool TryAddComboHelper(EntityUid uid, string helperPrototypeId, bool enabled = true)
    {
        // Перевіряємо, чи існує прототип
        if (!_proto.HasIndex<CqcComboHelperPrototype>(helperPrototypeId))
        {
            Logger.WarningS("ComboHelper", $"Tried to add ComboHelper with non-existent prototype: {helperPrototypeId}");
            return false;
        }

        // Додаємо або отримуємо існуючий компонент
        var helper = EnsureComp<ComboHelperComponent>(uid);
        helper.Prototype = helperPrototypeId;
        helper.Enabled = enabled;

        Logger.DebugS("ComboHelper", $"Added ComboHelper to {uid} with prototype {helperPrototypeId}");
        Dirty(uid, helper);

        return true;
    }

    /// <summary>
    /// Видаляє ComboHelper компонент з entity.
    /// </summary>
    public void RemoveComboHelper(EntityUid uid)
    {
        if (RemComp<ComboHelperComponent>(uid))
        {
            Logger.DebugS("ComboHelper", $"Removed ComboHelper from {uid}");
        }
    }

    /// <summary>
    /// Оновлює прототип helper для існуючого ComboHelper компонента.
    /// </summary>
    public bool TryUpdateHelperPrototype(EntityUid uid, string newPrototypeId, ComboHelperComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
        {
            Logger.WarningS("ComboHelper", $"Tried to update helper prototype on {uid} without ComboHelperComponent");
            return false;
        }

        if (!_proto.HasIndex<CqcComboHelperPrototype>(newPrototypeId))
        {
            Logger.WarningS("ComboHelper", $"Tried to update to non-existent prototype: {newPrototypeId}");
            return false;
        }

        component.Prototype = newPrototypeId;
        Logger.DebugS("ComboHelper", $"Updated helper prototype for {uid} to {newPrototypeId}");

        Dirty(uid, component);
        return true;
    }

    /// <summary>
    /// Перевіряє, чи має entity активний ComboHelper.
    /// </summary>
    public bool HasActiveHelper(EntityUid uid, ComboHelperComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        return component.Enabled && component.Prototype != null;
    }

    /// <summary>
    /// Отримує ID прототипу helper для entity, якщо він існує.
    /// </summary>
    public string? GetHelperPrototypeId(EntityUid uid, ComboHelperComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return null;

        return component.Prototype;
    }
}