using System.ComponentModel.Design.Serialization;
using Sandbox;
using RogueRecoil.Modifiers;

public sealed class ArenaPlayer : Component
{
	[Property] public int PlayerIndex { get; set; }

	private PlayerController controller;
	private Health health;

	protected override void OnStart()
	{
		controller = GetComponent<PlayerController>();
		health = GetComponent<Health>();

		if ( controller == null )
		{
			Log.Warning( "No Player Controller Found! Did you attach it?" );
		}

		if ( health == null )
		{
			Log.Warning( "No Health Component Found! Did you attach it?" );
		}
		else
		{
			health.OnDeath += OnDeath;
		}
	}

	private void OnDeath( ArenaPlayer victim )
    {
		Log.Info( $"Player {victim.PlayerIndex} has died." );
		GameManager.Instance?.EndRound();
    }
	
	public void ApplyModifier(IModifier modifier)
    {
		modifier.Apply( this );
    }
}
