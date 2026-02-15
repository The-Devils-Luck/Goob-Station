// SPDX-FileCopyrightText: 2026 GoobBot <uristmchands@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Shitmed.Targeting;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Pirate.Movement.Pulling.Events;

[Serializable, NetSerializable]
public sealed partial class ThroatSliceDoAfterEvent(TargetBodyPart targetPart) : DoAfterEvent
{
    public TargetBodyPart TargetPart = targetPart;

    public ThroatSliceDoAfterEvent() : this(TargetBodyPart.Head)
    {
    }

    public override DoAfterEvent Clone() => this;
}
