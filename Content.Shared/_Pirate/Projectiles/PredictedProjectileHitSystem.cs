using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared._Pirate.Projectiles;

public sealed class PredictedProjectileHitSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedGunSystem _guns = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedProjectileSystem _projectiles = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        if (_net.IsClient)
            SubscribeLocalEvent<ProjectileComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(Entity<ProjectileComponent> ent, ref StartCollideEvent args)
    {
        if (!_net.IsClient ||
            !_timing.IsFirstTimePredicted ||
            args.OurFixtureId != SharedProjectileSystem.ProjectileFixture ||
            !args.OtherFixture.Hard ||
            ent.Comp.ProjectileSpent ||
            ent.Comp is { Weapon: null, OnlyCollideWhenShot: true })
        {
            return;
        }

        var reflectEv = new ProjectileReflectAttemptEvent(ent, ent.Comp, false);
        RaiseLocalEvent(args.OtherEntity, ref reflectEv);
        if (reflectEv.Cancelled)
        {
            _projectiles.SetShooter(ent, ent.Comp, args.OtherEntity);
            _guns.SetTarget(ent, null, out _);
            ent.Comp.IgnoredEntities.Clear();
            return;
        }

        var hitEv = new ProjectileHitEvent(
            ent.Comp.Damage * _damageable.UniversalProjectileDamageModifier,
            args.OtherEntity,
            ent.Comp.Shooter);
        RaiseLocalEvent(ent, ref hitEv);

        ent.Comp.ProjectileSpent = !ent.Comp.Penetrate;
        if (ent.Comp.Penetrate)
            ent.Comp.IgnoredEntities.Add(args.OtherEntity);

        if ((ent.Comp.DeleteOnCollide && ent.Comp.ProjectileSpent) ||
            (ent.Comp.NoPenetrateMask & args.OtherFixture.CollisionLayer) != 0)
        {
            var deleteEv = new DeletingProjectileEvent(ent.Owner);
            RaiseLocalEvent(ref deleteEv);
            PredictedQueueDel(ent.Owner);
        }
    }
}
