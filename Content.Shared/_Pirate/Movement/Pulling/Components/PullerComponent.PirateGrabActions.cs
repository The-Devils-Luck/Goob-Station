// SPDX-FileCopyrightText: 2026 GoobBot <uristmchands@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

namespace Content.Shared.Movement.Pulling.Components;

public sealed partial class PullerComponent
{
    [DataField]
    public TimeSpan BoneBreakDelay = TimeSpan.FromSeconds(4.5f);

    [DataField]
    public float BoneBreakBaseChance = 0.6f;

    [DataField]
    public float BoneBreakChancePerBluntDamage = 0.02f;

    [DataField]
    public float BoneBreakMaxChance = 0.95f;

    [DataField]
    public float BoneBreakBaseDamage = 60f;

    [DataField]
    public float BoneBreakDamagePerBluntDamage = 1f;

    [DataField]
    public float BoneBreakStrikeMultiplier = 2f;

    [DataField]
    public float BoneBreakTraumaWoundSeverity = 8f;

    [DataField]
    public TimeSpan ThroatSliceDelay = TimeSpan.FromSeconds(4.5f);

    [DataField]
    public float ThroatSliceBaseSeverity = 20f;

    [DataField]
    public float ThroatSliceSeverityPerSharpDamage = 1f;

    [DataField]
    public float ThroatSliceMinimumBleed = 12f;

    [DataField]
    public float ThroatSliceStrikeMultiplier = 2.5f;
}
