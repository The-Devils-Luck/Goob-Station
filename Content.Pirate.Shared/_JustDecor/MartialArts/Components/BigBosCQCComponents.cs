using Content.Goobstation.Common.MartialArts;
using Content.Goobstation.Shared.MartialArts.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Pirate.Shared._JustDecor.MartialArts.Components;

/// <summary>
/// Компонент для отримання знання BigBos CQC
/// </summary>
[RegisterComponent]
public sealed partial class GrantBigBosCQCComponent : GrantMartialArtKnowledgeComponent
{
    [DataField]
    public override MartialArtsForms MartialArtsForm { get; set; } = MartialArtsForms.BigBosCloseQuartersCombat;

    public override LocId? LearnMessage { get; set; } = "bigbos-cqc-success-learned";
}


/// <summary>
/// Компонент для бафу кулдаунів BigBos CQC
/// </summary>
[RegisterComponent]
public sealed partial class BigBosCqcCooldownsComponent : Component
{
    [DataField]
    public Dictionary<string, TimeSpan> CooldownTimers = new();
}

/// <summary>
/// Компонент для бафу "знання" BigBos CQC
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BigBosCqcKnowledgeComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public int CombosPerformed = 0;

    [DataField]
    public TimeSpan LastComboTime;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public bool CombatMode = false;

    [DataField]
    public TimeSpan CombatModeDuration = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan CombatModeEndTime;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public int CurrentComboChain = 0;

    [DataField]
    public int MaxComboChain = 3;

    [DataField]
    public TimeSpan ComboChainTimeout = TimeSpan.FromSeconds(2);

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float HasteMeter = 0f;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float MaxHasteBonus = 0.8f; // Max 60% speed bonus (was 40%)

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float HasteDecayRate = 0.1f; // Decay per second (was 0.2)

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float? OriginalAttackRate;
}

/// <summary>
/// Компонент для бафу Rush (збільшення швидкості)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BigBosCqcRushBuffComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan EndTime;

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 1.3f;
}

/// <summary>
/// Компонент для бафу (зменшення/відбиття шкоди)
/// </summary>
[RegisterComponent]
public sealed partial class BigBosCqcCounterBuffComponent : Component
{
    [DataField]
    public TimeSpan EndTime;

    [DataField]
    public float DamageReduction = 0.5f;

    [DataField]
    public float ReflectDamage = 0.2f;

    [DataField]
    public SoundSpecifier? CounterSound;
}

/// <summary>
/// Компонент для удушення
/// </summary>
[RegisterComponent]
public sealed partial class InChokeholdComponent : Component
{
    [DataField]
    public EntityUid Choker;

    [DataField]
    public float OxygenDamageRate = 2f;

    [DataField]
    public TimeSpan LastDamageTick;
}
