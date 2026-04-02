using Robust.Shared.GameStates;

namespace Content.Pirate.Shared.Witch.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class WitchInvisibilityComponent : Component
{
    [DataField]
    public float Visibility = -1.5f;
}
