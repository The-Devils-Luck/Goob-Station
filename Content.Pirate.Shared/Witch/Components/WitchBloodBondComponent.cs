using Robust.Shared.GameStates;

namespace Content.Pirate.Shared.Witch.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class WitchBloodBondComponent : Component
{
    [DataField]
    public float Radius = 3f;
}
