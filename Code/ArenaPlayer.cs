using System.ComponentModel.Design.Serialization;
using Sandbox;
using RogueRecoil.Modifiers;

public sealed class ArenaPlayer : Component, Component.INetworkListener
{
	[Property] public int PlayerIndex { get; set; }

	private PlayerController controller;
	private Health health;

	protected override void OnStart()
	{
		controller = GetComponent<PlayerController>();
		health = GetComponent<Health>();

		PlayerVisibility();

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

	public void ApplyModifier( IModifier modifier )
	{
		modifier.Apply( this );
	}

	[Rpc.Broadcast]
	public void TeleportPlayer( Vector3 position, Rotation rotation )
	{
		GameObject.WorldPosition = position;
		GameObject.WorldRotation = rotation;

		var controller = Components.Get<PlayerController>();

	}

	public void PlayerVisibility()
	{

		var isMe = Network.IsCreator;
        if ( isMe == true )
        {
			var render = GameObject.GetComponent<PlayerController>();

			var modelHide = render?.HideBodyInFirstPerson == true;

			if ( modelHide == false )
			{
				render.HideBodyInFirstPerson = true;
			}
		}
	
    }

}
