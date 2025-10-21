using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathF = System.MathF;

public enum GameState
{
    Waiting,
    RoundStarting,
    InProgress,
    RoundOver,
    MatchOver
}

public class GameManager : Component, Component.INetworkListener
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
    [Property] public GameObject SpawnPoint1 { get; set; }
    [Property] public GameObject SpawnPoint2 { get; set; }

    private readonly List<Connection> activeConnections = new();
    private readonly Dictionary<Connection, ArenaPlayer> players = new();

    private ArenaPlayer player1;
    private ArenaPlayer player2;

    public void OnActive(Connection connection)
    {
        activeConnections.Add(connection);
        SpawnPlayerForConnection(connection);
    }

    protected override void OnStart()
    {
        Instance = this;
        Log.Info("GameManager initialized");
    }

    protected override void OnUpdate()
    {
        if (State == GameState.InProgress)
            UpdateRoundTimer();
    }

    public void SpawnPlayerForConnection(Connection connection)
    {
        if (PlayerPrefab == null || SpawnPoint1 == null || SpawnPoint2 == null)
        {
            Log.Warning("Player Prefab or Spawn Points not set!");
            return;
        }

        // Assign spawn based on join order
        int index = Math.Clamp(Connection.All.ToList().IndexOf(connection), 0, 1);
        var spawnTransform = index == 0 ? SpawnPoint1.Transform.World : SpawnPoint2.Transform.World;

        GameObject playerObj = PlayerPrefab.Clone(spawnTransform.Position, spawnTransform.Rotation);
        playerObj.NetworkSpawn(connection);

        var arenaPlayer = playerObj.GetComponent<ArenaPlayer>();
        if (arenaPlayer != null)
        {
            arenaPlayer.PlayerIndex = index + 1;
            arenaPlayer.GetComponent<Health>()?.ResetHealth();
            players[connection] = arenaPlayer;

            if (index == 0) player1 = arenaPlayer;
            else player2 = arenaPlayer;

            Log.Info($"Spawned {connection.DisplayName} at Spawn {index + 1}");
        }
        else
        {
            Log.Warning($"Spawned player for {connection.DisplayName} but no ArenaPlayer component found!");
        }
    }

    public void OnDisconnected(Connection connection)
    {
        activeConnections.Remove(connection);
        players.Remove(connection);
        Log.Info($"{connection.DisplayName} left the match.");
    }

    public void StartNextRound()
    {
        CurrentRound++;
        State = GameState.RoundStarting;
        RoundTimer = RoundDuration;

        Log.Info($"Round {CurrentRound} starting!");

        // Wait a fraction before reset so all network objects are ready
        _ = DelayedResetAsync();
    }

    private async Task DelayedResetAsync()
    {
        await GameTask.DelaySeconds(0.1f); // allow network sync to settle
        ResetPlayersForRound();
        State = GameState.InProgress;
    }

    public void EndRound(ArenaPlayer winner = null)
    {
        if (State != GameState.InProgress)
            return;

        State = GameState.RoundOver;

        if (winner != null)
            Log.Info($"Round over! Winner: Player {winner.PlayerIndex}");
        else
            Log.Info("Round over! It's a draw!");

        ResetPlayersForRound();
        CheckMatchOver();

        StartNextRound();
    }

    private void UpdateRoundTimer()
    {
        RoundTimer -= Time.Delta;
        if (RoundTimer <= 0f)
        {
            RoundTimer = 0f;
            EndRound();
        }
    }

    private void CheckMatchOver()
    {
        if (CurrentRound >= MaxRounds)
        {
            State = GameState.MatchOver;
            Log.Info("Match over!");
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
        Log.Info("Match reset, waiting for players...");
    }

    public void ResetPlayersForRound()
    {
        foreach (var (connection, ap) in players)
        {
            if (ap == null || ap.GameObject?.IsValid != true)
                continue;

            ap.GetComponent<Health>()?.ResetHealth();

            var targetSpawn = ap.PlayerIndex == 1 ? SpawnPoint1 : SpawnPoint2;
            if (targetSpawn == null)
                continue;

            ap.TeleportPlayer(targetSpawn.Transform.World.Position, targetSpawn.Transform.World.Rotation);
        }

        Log.Info("Players reset for new round.");
    }

    // Console Commands
    [ConCmd("gm.start")]
    public static void CmdStartRound()
    {
        if (Instance == null)
        {
            Log.Warning("No GameManager instance in scene!");
            return;
        }
        Instance.StartNextRound();
    }

    [ConCmd("gm.end")]
    public static void CmdEndRound() => Instance?.EndRound();

    [ConCmd("gm.state")]
    public static void CmdPrintState()
    {
        if (Instance == null) return;
        Log.Info($"State = {Instance.State}, Round = {Instance.CurrentRound}, Timer = {Instance.RoundTimer}");
    }
}
