using System;
using Robust.Shared.GameStates;

namespace Content.Pirate.Shared.Witch.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class WitchSubstitutionStatusEffectComponent : Component
{
    [DataField]
    public float Range = 6f;

    [DataField]
    public float FallbackDamage = 5f;

    [DataField]
    public float Interval = 1.5f;

    [DataField]
    public TimeSpan NextAttempt;
}
