using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Pirate.Shared.Alchemy.Components;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Pirate.Server.Alchemy.Systems;

// Slowly ruins worn gear and spills bag contents first.
public sealed class AlchemistSoulCopperasSystem : EntitySystem
{
    private const SlotFlags CorrodedSlots =
        SlotFlags.HEAD |
        SlotFlags.EYES |
        SlotFlags.EARS |
        SlotFlags.MASK |
        SlotFlags.OUTERCLOTHING |
        SlotFlags.INNERCLOTHING |
        SlotFlags.NECK |
        SlotFlags.BACK |
        SlotFlags.BELT |
        SlotFlags.GLOVES |
        SlotFlags.LEGS |
        SlotFlags.FEET |
        SlotFlags.SUITSTORAGE;

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, float> _corrosion = [];

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AlchemistSoulCopperasComponent, InventoryComponent>();
        while (query.MoveNext(out var uid, out var comp, out var inventory))
        {
            if (_timing.CurTime < comp.NextUpdate)
                continue;

            comp.NextUpdate = _timing.CurTime + TimeSpan.FromSeconds(comp.Interval);

            var candidates = new List<EntityUid>();
            var enumerator = _inventory.GetSlotEnumerator((uid, inventory), CorrodedSlots);
            while (enumerator.NextItem(out var item))
            {
                if (Deleted(item) || Terminating(item))
                    continue;

                candidates.Add(item);
            }

            if (candidates.Count == 0)
                continue;

            var target = _random.Pick(candidates);
            if (TryComp<DamageableComponent>(target, out _))
            {
                var damage = new DamageSpecifier();
                damage.DamageDict["Caustic"] = FixedPoint2.New(comp.DamagePerTick);
                damage.DamageDict["Heat"] = FixedPoint2.New(comp.DamagePerTick * 0.5f);
                _damageable.TryChangeDamage(target, damage, true, origin: uid, ignoreBlockers: true);
                continue;
            }

            _corrosion[target] = _corrosion.GetValueOrDefault(target) + comp.DamagePerTick;
            if (_corrosion[target] < comp.CorrodeThreshold)
                continue;

            _corrosion.Remove(target);
            SpillContainedItems(target);
            QueueDel(target);
        }

        foreach (var item in _corrosion.Keys.ToArray())
        {
            if (!Exists(item) || Deleted(item) || Terminating(item))
                _corrosion.Remove(item);
        }
    }

    private void SpillContainedItems(EntityUid uid)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
            return;

        // Spill contents before the container disappears.
        var coordinates = Transform(uid).Coordinates;
        foreach (var container in _container.GetAllContainers(uid, containerManager))
        {
            _container.EmptyContainer(container, true, coordinates);
        }
    }
}
