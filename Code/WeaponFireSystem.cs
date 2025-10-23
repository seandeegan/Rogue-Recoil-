using Sandbox;
using System.Threading.Tasks;

public sealed class WeaponFireSystem : Component
{
    [Property] public WeaponBase WeaponBase { get; set; }
    [Property] public GameObject ImpactEffect { get; set; }

    [Property] public float Range { get; set; } = 5000f;
    [Property] public float Damage { get; set; } = 25f;
    [Property] public float Spread { get; set; } = 0.1f;
    [Property] public int Pellets { get; set; } = 1;
    [Property] public float SpreadAngle { get; set; } = 5f; // degrees for shotguns

    [Rpc.Broadcast]
    public void RpcSpawnImpact(Vector3 pos, Vector3 normal)
    {
        if (ImpactEffect != null)
        {
            var fx = ImpactEffect.Clone(pos, Rotation.LookAt(normal));
            _ = DeleteAfterDelay(fx, 1.5f);
        }
    }

    private async Task DeleteAfterDelay(GameObject obj, float delay)
    {
        await Task.DelaySeconds(delay);
        if (obj.IsValid())
            obj.Destroy();
    }

    [Rpc.Host]
    public void RpcHostFire(Vector3 start, Vector3 direction)
    {
        if (WeaponBase == null)
            return;

        int pelletCount = WeaponBase.Type == WeaponType.Shotgun ? Pellets : 1;

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 dir = direction;

            // Apply random spread for shotguns
            if (WeaponBase.Type == WeaponType.Shotgun)
                dir = ApplySpread(direction);

            var trace = Scene.Trace
                .Ray(start, start + dir * Range)
                .UseHitboxes()
                .IgnoreGameObject(WeaponBase.GameObject)
                .Radius(Spread)
                .Run();

            if (trace.Hit)
            {
                var health = trace.GameObject?.Components.Get<Health>(true);
                health?.TakeDamage(Damage, null);

                RpcSpawnImpact(trace.HitPosition, trace.Normal);
            }
        }
    }

    public void Fire()
    {
        var muzzle = WeaponBase.MuzzleAttachment;
        var start = muzzle?.WorldPosition ?? WeaponBase.WorldPosition;
        var dir = WeaponBase.WorldRotation.Forward;

        RpcHostFire(start, dir);
    }

    private Vector3 ApplySpread(Vector3 forward)
    {
        // Random cone spread
        var spreadRot = Rotation.From(
            new Angles(
                Game.Random.Float(-SpreadAngle, SpreadAngle),
                Game.Random.Float(-SpreadAngle, SpreadAngle),
                0
            )
        );

        return (spreadRot * forward).Normal;
    }
}
