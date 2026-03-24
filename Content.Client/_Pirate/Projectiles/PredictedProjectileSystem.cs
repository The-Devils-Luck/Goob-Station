using Content.Shared.Projectiles;
using Content.Shared._Pirate.Projectiles;
using Robust.Client.GameObjects;

namespace Content.Client._Pirate.Projectiles;

public sealed class PredictedProjectileSystem : EntitySystem
{
    [Dependency] private readonly Robust.Client.Physics.PhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileComponent, Robust.Client.Physics.UpdateIsPredictedEvent>(OnUpdateIsPredicted);
        SubscribeLocalEvent<DeletingProjectileEvent>(OnDeletingProjectile);
        SubscribeNetworkEvent<ShotPredictedProjectileEvent>(OnShotPredictedProjectile);
    }

    private void OnUpdateIsPredicted(Entity<ProjectileComponent> ent, ref Robust.Client.Physics.UpdateIsPredictedEvent args)
    {
        args.IsPredicted = true;
    }

    private void OnDeletingProjectile(ref DeletingProjectileEvent args)
    {
        RemComp<SpriteComponent>(args.Entity);
        RemComp<PointLightComponent>(args.Entity);
    }

    private void OnShotPredictedProjectile(ShotPredictedProjectileEvent args)
    {
        var uid = GetEntity(args.Projectile);
        if (!uid.IsValid())
            return;

        _physics.UpdateIsPredicted(uid);
    }
}
