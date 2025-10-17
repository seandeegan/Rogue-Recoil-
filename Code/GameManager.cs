using Sandbox;



public enum GameState
{
	Waiting,
	RoundStarting,
	InProgress,
	RoundOver,
	MatchOver
}

public class GameManager : Component
{
	public static GameManager Instance { get; private set; }

	public GameState State { get; private set; } = GameState.Waiting;
	public int CurrentRound { get; private set; } = 0;
	public int MaxRounds { get; private set; } = 10;

	private float RoundTimer = 0f;
	private float RoundDuration = 30f;

	[Property] public int Player1Score { get; private set; }
	[Property] public int Player2Score { get; private set; }

	[Property] public GameObject PlayerPrefab { get; set; }

	private ArenaPlayer player1;
	private ArenaPlayer player2;


	
	protected override void OnStart()
	{
		Instance = this;
		Log.Info( "GameManager initialized" );
		// StartNextRound();
	}


	protected override void OnUpdate()
	{
		switch ( State )
		{
			case GameState.Waiting:
				// Check if enough players to start the game

				// UpdateRoundTimer();
				break;

			case GameState.InProgress:
				UpdateRoundTimer();
				break;
		}
	}

	public void SpawnPlayers()
    {
		//var spawnPoints = Entity.All.OfType<SpawnPoint>().ToList();
		// THIS IS THE LAST THING WE WROTE
		//
		//
		//
    }


	public void StartNextRound()
	{
		CurrentRound++;
		State = GameState.RoundStarting;
		RoundTimer = RoundDuration;
		// Reset player positions, health, etc.
		// Notify players of new round
		Log.Info( $"Round {CurrentRound} starting!" );
		State = GameState.InProgress;
	}
	
	
    public void EndRound()
    {
		State = GameState.RoundOver;
		// Calculate scores, notify players
		CheckMatchOver();
		StartNextRound();
    }
    private void UpdateRoundTimer()
    {
		if ( State == GameState.InProgress )
		{


			RoundTimer = RoundTimer - Time.Delta;

			if ( RoundTimer <= 0f)
            {
				RoundTimer = 0f;
				EndRound();
            }
		}
    }
	private void CheckMatchOver()
	{
		if ( CurrentRound >= MaxRounds )
		{
			State = GameState.MatchOver;

			// Determine overall winner, notify players
			Log.Info( "Match over!" );
			// Reset for next match
			EndMatch();
		}
	}

	private void EndMatch()
	{
		State = GameState.Waiting;
		CurrentRound = 0;
		Player1Score = 0;
		Player2Score = 0;
		RoundTimer = 0f;
		// Reset player states, prepare for new match
		Log.Info( "Match reset, waiting for players..." );
	}




// Console Commands for testing and debugging, can be removed in production and CHANGE METHODS BACK TO PRIVATE!!!!!

	[ConCmd( "gm.start" )]
public static void CmdStartRound()
{
    if ( Instance == null )
    {
        Log.Warning( "No GameManager instance in scene!" );
        return;
    }
    Instance.StartNextRound();
}

[ConCmd("gm.end")]
public static void CmdEndRound()
{
    if ( Instance == null ) return;
    Instance.EndRound();
}

[ConCmd("gm.state")]
public static void CmdPrintState()
{
    if ( Instance == null ) return;
    Log.Info( $"State = {Instance.State}, Round = {Instance.CurrentRound}, Timer = {Instance.RoundTimer}" );
}

}
