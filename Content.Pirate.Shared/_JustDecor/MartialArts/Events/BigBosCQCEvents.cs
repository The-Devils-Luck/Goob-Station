using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Pirate.Shared._JustDecor.MartialArts.Events;

/// <summary>
/// Івенти для комбо
/// </summary>

public sealed partial class BigBosCqcTakedownPerformedEvent : EntityEventArgs { }

public sealed partial class BigBosCqcDisarmPerformedEvent : EntityEventArgs { }

public sealed partial class BigBosCqcThrowPerformedEvent : EntityEventArgs { }

public sealed partial class BigBosCqcChokePerformedEvent : EntityEventArgs { }

public sealed partial class BigBosCqcChainPerformedEvent : EntityEventArgs { }

public sealed partial class BigBosCqcCounterPerformedEvent : EntityEventArgs { }

public sealed partial class BigBosCqcInterrogationPerformedEvent : EntityEventArgs { }

public sealed partial class BigBosCqcStealthTakedownPerformedEvent : EntityEventArgs { }

public sealed partial class BigBosCqcRushPerformedEvent : EntityEventArgs { }

public sealed partial class BigBosCqcFinisherPerformedEvent : EntityEventArgs { }

// DoAfter івенти для подовжених комбошок
[Serializable, NetSerializable]
public sealed partial class BigBosCqcInterrogationDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class BigBosCqcChokeDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
