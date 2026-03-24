using Content.Shared.Projectiles;
using Content.Shared._Pirate.Projectiles;
namespace Content.Client._Pirate.Projectiles;

public sealed class PredictedProjectileSystem : EntitySystem
{
    [Dependency] private readonly Robust.Client.Physics.PhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileComponent, Robust.Client.Physics.UpdateIsPredictedEvent>(OnUpdateIsPredicted);
        SubscribeNetworkEvent<ShotPredictedProjectileEvent>(OnShotPredictedProjectile);
    }

    private void OnUpdateIsPredicted(Entity<ProjectileComponent> ent, ref Robust.Client.Physics.UpdateIsPredictedEvent args)
    {
        args.IsPredicted = true;
    }

    private void OnShotPredictedProjectile(ShotPredictedProjectileEvent args)
    {
        var uid = GetEntity(args.Projectile);
        if (!uid.IsValid())
            return;

        _physics.UpdateIsPredicted(uid);
    }
}
