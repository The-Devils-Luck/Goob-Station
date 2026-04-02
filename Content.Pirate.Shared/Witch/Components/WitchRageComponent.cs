using Robust.Shared.GameStates;

namespace Content.Pirate.Shared.Witch.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WitchRageComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Faction = "SimpleHostile";
}
