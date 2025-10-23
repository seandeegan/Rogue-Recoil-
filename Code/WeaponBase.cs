using Sandbox;
using System;
using System.Threading.Tasks;

public enum WeaponState
{
    Idle,
    Reloading,
    Empty,
    Sprinting,
    Jumping
}

public enum WeaponType
{
    Pistol,
    Shotgun,
    Rifle,
    Suppressed
}

public sealed class WeaponBase : Component
{
    [Property] public int MaxAmmo { get; set; } = 90;
    [Property] public int ClipSize { get; set; } = 30;
    [Property] public bool TwoHanded { get; set; } = true;
    [Property] public SkinnedModelRenderer ViewModel { get; set; }

    [Property] public float FireRate { get; set; } = 0.2f;
    [Property] public float ReloadTime { get; set; } = 1.6f;
    [Property] public float SprintRecoverDelay { get; set; } = 0.25f;
    [Property] public float AimTransitionSpeed { get; set; } = 10f;

    [Property] public WeaponType Type { get; set; } = WeaponType.Rifle;

    // 🔥 Muzzle flashes
    [Property] public GameObject PistolMuzzleFlash { get; set; }
    [Property] public GameObject ShotgunMuzzleFlash { get; set; }
    [Property] public GameObject RifleMuzzleFlash { get; set; }
    [Property] public GameObject SuppressedMuzzleFlash { get; set; }

    // 🔊 FIRE sounds
    [Property] public SoundEvent PistolShotSound { get; set; }
    [Property] public SoundEvent ShotgunShotSound { get; set; }
    [Property] public SoundEvent RifleShotSound { get; set; }
    [Property] public SoundEvent SuppressedShotSound { get; set; }

    // 🔄 RELOAD sounds
    [Property] public SoundEvent PistolReloadSound { get; set; }
    [Property] public SoundEvent ShotgunReloadSound { get; set; }
    [Property] public SoundEvent RifleReloadSound { get; set; }
    [Property] public SoundEvent SuppressedReloadSound { get; set; }

	// 💨 Muzzle smoke
	[Property] public GameObject PistolSmoke { get; set; }
	[Property] public GameObject ShotgunSmoke { get; set; }
	[Property] public GameObject RifleSmoke { get; set; }
	[Property] public GameObject SuppressedSmoke { get; set; }


    [Property] public GameObject MuzzleAttachment { get; set; }

    private int ammoInClip;
    private int ammoReserve;
    private WeaponState state;
    private TimeSince timeSinceLastShot;
    private TimeSince timeSinceSprintEnd;
    private float adsBlend;

    protected override void OnStart()
    {
        ammoInClip = ClipSize;
        ammoReserve = MaxAmmo - ClipSize;
        state = WeaponState.Idle;
        UpdatePersistentParams();

        if (PistolMuzzleFlash != null) PistolMuzzleFlash.Enabled = false;
        if (ShotgunMuzzleFlash != null) ShotgunMuzzleFlash.Enabled = false;
        if (RifleMuzzleFlash != null) RifleMuzzleFlash.Enabled = false;
        if (SuppressedMuzzleFlash != null) SuppressedMuzzleFlash.Enabled = false;
    }

    protected override void OnUpdate()
    {
        HandleSprinting();
        HandleAiming();

        if (Input.Pressed("reload"))
        {
            TryReload();
            return;
        }

        if (Input.Down("attack1") && state != WeaponState.Sprinting)
            TryFire();

        UpdatePersistentParams();
    }

    private void HandleSprinting()
    {
        bool wantsToSprint = Input.Down("run") && state != WeaponState.Reloading;

        if (wantsToSprint)
        {
            if (state != WeaponState.Sprinting)
            {
                state = WeaponState.Sprinting;
                ViewModel?.Set("b_sprint", true);
                ViewModel?.Set("move_bob", 0.6f);
            }
        }
        else if (state == WeaponState.Sprinting)
        {
            state = WeaponState.Idle;
            ViewModel?.Set("b_sprint", false);
            ViewModel?.Set("move_bob", 0.0f);
            timeSinceSprintEnd = 0;
        }
    }

    private void HandleAiming()
    {
        bool wantsAim = Input.Down("attack2") && state != WeaponState.Reloading && state != WeaponState.Sprinting;

        float target = wantsAim ? 1f : 0f;
        adsBlend = adsBlend.LerpTo(target, Time.Delta * AimTransitionSpeed);

        if (ViewModel != null)
        {
            ViewModel.Set("ironsights", adsBlend >= 0.5f ? 1 : 0);
            ViewModel.Set("ironsights_fire_scale", adsBlend);
        }
    }

    private void TryFire()
    {
        if (timeSinceSprintEnd < SprintRecoverDelay) return;
        if (state == WeaponState.Reloading) return;

        if (ammoInClip <= 0)
        {
            state = WeaponState.Empty;
            ViewModel?.Set("b_attack_dry", true);
            return;
        }

        if (timeSinceLastShot < FireRate) return;

        timeSinceLastShot = 0;
        ammoInClip--;

        ViewModel?.Set("b_attack", true);
        Shoot();

        if (ammoInClip <= 0)
            state = WeaponState.Empty;
    }

    private void TryReload()
    {
        if (state == WeaponState.Reloading) return;
        if (ammoReserve <= 0 || ammoInClip == ClipSize) return;

        state = WeaponState.Reloading;
        ViewModel?.Set("b_reload", true);
        PlayReloadSound();
        _ = ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        await Task.DelaySeconds(ReloadTime);

        int needed = ClipSize - ammoInClip;
        int taken = Math.Min(needed, ammoReserve);

        ammoInClip += taken;
        ammoReserve -= taken;
        state = WeaponState.Idle;

        UpdatePersistentParams();
    }

	private void Shoot()
	{
		_ = SpawnMuzzleFlash();
		_ = PlayShotSound();
		_ = SpawnSmoke();

	}

	private async Task SpawnSmoke()
{
    GameObject fx = Type switch
    {
        WeaponType.Pistol => PistolSmoke,
        WeaponType.Shotgun => ShotgunSmoke,
        WeaponType.Suppressed => SuppressedSmoke,
        _ => RifleSmoke
    };

    if (fx == null) return;

    if (MuzzleAttachment != null)
    {
        fx.WorldPosition = MuzzleAttachment.WorldPosition;
        fx.WorldRotation = MuzzleAttachment.WorldRotation;
    }
    else
    {
        fx.WorldPosition = WorldPosition + WorldRotation.Forward * 10f;
        fx.WorldRotation = WorldRotation;
    }

    fx.Enabled = true;
    await Task.DelaySeconds(9f); // smoke lingers longer
    if (fx.IsValid())
        fx.Enabled = false;
}

    private async Task SpawnMuzzleFlash()
    {
        GameObject fx = Type switch
        {
            WeaponType.Pistol => PistolMuzzleFlash,
            WeaponType.Shotgun => ShotgunMuzzleFlash,
            WeaponType.Suppressed => SuppressedMuzzleFlash,
            _ => RifleMuzzleFlash
        };

        if (fx == null) return;

        if (MuzzleAttachment != null)
        {
            fx.WorldPosition = MuzzleAttachment.WorldPosition;
            fx.WorldRotation = MuzzleAttachment.WorldRotation;
        }
        else
        {
            fx.WorldPosition = WorldPosition + WorldRotation.Forward * 10f;
            fx.WorldRotation = WorldRotation;
        }

        fx.Enabled = true;
        await Task.DelaySeconds(0.15f);

        if (fx.IsValid())
            fx.Enabled = false;
    }

    private async Task PlayShotSound()
    {
        SoundEvent sound = Type switch
        {
            WeaponType.Pistol => PistolShotSound,
            WeaponType.Shotgun => ShotgunShotSound,
            WeaponType.Suppressed => SuppressedShotSound,
            _ => RifleShotSound
        };

        if (sound == null)
            return;

        Vector3 pos = MuzzleAttachment?.WorldPosition ?? WorldPosition;
        Sound.Play(sound, pos);
        await Task.CompletedTask;
    }

    private void PlayReloadSound()
    {
        SoundEvent sound = Type switch
        {
            WeaponType.Pistol => PistolReloadSound,
            WeaponType.Shotgun => ShotgunReloadSound,
            WeaponType.Suppressed => SuppressedReloadSound,
            _ => RifleReloadSound
        };

        if (sound == null)
            return;

        Vector3 pos = MuzzleAttachment?.WorldPosition ?? WorldPosition;
        Sound.Play(sound, pos);
    }

    private void UpdatePersistentParams()
    {
        if (ViewModel == null) return;
        ViewModel.Set("b_twohanded", TwoHanded);
        ViewModel.Set("b_empty", ammoInClip <= 0);
    }

    public int AmmoInClip => ammoInClip;
    public int AmmoReserve => ammoReserve;
    public WeaponState State => state;
}
