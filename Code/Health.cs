using System;
using Sandbox;

public sealed class Health : Component
{
	[Property] public float MaxHealth { get; set; } = 100f;
	[Property] public float CurrentHealth { get; private set; }

	[Property] public bool IsDead => CurrentHealth <= 0f;

	public event Action<ArenaPlayer> OnDeath;

	protected override void OnStart()
	{
		CurrentHealth = MaxHealth;
	}

	public void TakeDamage( float amount, ArenaPlayer attacker = null )
	{
		if ( IsDead ) return;

		CurrentHealth -= amount;
		if ( CurrentHealth <= 0f )
		{
			Die( attacker );
		}

	}

	public void Die( ArenaPlayer attacker )
	{
		CurrentHealth = 0f;
		var victim = GetComponent<ArenaPlayer>();

		OnDeath?.Invoke( victim );

		if ( attacker != null )
		{
			Log.Info( $"Player {victim.PlayerIndex} was killed by Player {attacker.PlayerIndex}." );
		}
        else
        {
            Log.Info( $"Player {victim.PlayerIndex} died." );
        }
		

	}
	
	public void ResetHealth()
	{
		CurrentHealth = MaxHealth;
	}
}
