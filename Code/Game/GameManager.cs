using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public sealed class GameManager : Component, Component.INetworkListener
{
	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public Vector3 LobbySpawnPosition { get; set; } = new Vector3(0, 0, 100);
	[Property] public Vector3 FallbackSpawnPosition { get; set; } = new Vector3(0, 0, 50);
	[Property] public float StartCountdownDuration { get; set; } = 3f;

	[Sync(SyncFlags.FromHost)] public int StateInt { get; set; } = (int)LobbyState.Lobby;
	[Sync(SyncFlags.FromHost)] public float CountdownTimer { get; set; }

	public static GameManager Instance { get; private set; }
	public LobbyState State
	{
		get => (LobbyState)StateInt;
		set => StateInt = (int)value;
	}
	protected override void OnAwake()
	{
		Instance = this;
	}

	protected override async Task OnLoad()
	{
		if (Scene.IsEditor) return;

		if (!Networking.IsActive)
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds(0.1f);
			Networking.CreateLobby();
		}
	}

	void INetworkListener.OnActive(Connection channel)
	{
		if (!Networking.IsHost) return;
		if (PlayerPrefab is null) return;

		// Tous les joueurs spawnent dans la lobby zone, sans rôle
		var playerObject = PlayerPrefab.Clone(LobbySpawnPosition);
		playerObject.Name = $"Player - {channel.DisplayName}";

		// Pas de rôle assigné → reste PlayerRole.None pendant le lobby
		var setup = playerObject.GetComponent<PlayerSetup>();
		if (setup != null)
		{
			setup.AssignedRole = PlayerRole.None;
		}

		playerObject.NetworkSpawn(channel);
		Log.Info($"{channel.DisplayName} joined the lobby");
	}

	protected override void OnUpdate()
	{
		if (!Networking.IsHost) return;

		if (State == LobbyState.Starting)
		{
			CountdownTimer -= Time.Delta;
			if (CountdownTimer <= 0f)
			{
				ActuallyStartGame();
			}
		}
	}

	/// <summary>
	/// Appelé quand le host clique "Start Game" dans la lobby UI.
	/// </summary>
	public void RequestStartGame()
	{
		if (!Networking.IsHost) return;
		if (State != LobbyState.Lobby) return;

		var players = GetAllPlayers();
		if (players.Count == 0)
		{
			Log.Warning("No players to start with");
			return;
		}

		State = LobbyState.Starting;
		CountdownTimer = StartCountdownDuration;
		Log.Info($"Starting game in {StartCountdownDuration}s");
	}

	private void ActuallyStartGame()
	{
		State = LobbyState.InGame;

		var players = GetAllPlayers();
		if (players.Count == 0) return;

		// TIRAGE ALEATOIRE DU TUEUR
		int killerIndex = Random.Shared.Int(0, players.Count - 1);
		var killerPlayer = players[killerIndex];

		Log.Info($"Killer chosen: {killerPlayer.GameObject.Name}");

		// Assigne les rôles + téléporte aux spawn points
		for (int i = 0; i < players.Count; i++)
		{
			var p = players[i];
			PlayerRole role = (i == killerIndex) ? PlayerRole.Killer : PlayerRole.Survivor;

			p.Role = role;
			p.AssignedRole = role;

			Vector3 spawnPos = FindSpawnPosition(role);
			Rotation spawnRot = FindSpawnRotation(role);

			TeleportPlayerRpc(p.GameObject.Id, spawnPos, spawnRot);
		}

		Log.Info("Game started!");
	}

	[Rpc.Broadcast]
	private void TeleportPlayerRpc(Guid playerId, Vector3 pos, Rotation rot)
	{
		var go = Scene.Directory.FindByGuid(playerId);
		if (go == null) return;

		go.WorldPosition = pos;
		go.WorldRotation = rot;
	}

	public List<PlayerSetup> GetAllPlayers()
	{
		return Scene.GetAllComponents<PlayerSetup>().ToList();
	}

	private Vector3 FindSpawnPosition(PlayerRole role)
	{
		var spawns = Scene.GetAllComponents<SpawnPoint>()
			.Where(sp => sp.Role == role)
			.ToList();

		if (spawns.Count == 0) return FallbackSpawnPosition;
		return spawns[Random.Shared.Int(0, spawns.Count - 1)].WorldPosition;
	}

	private Rotation FindSpawnRotation(PlayerRole role)
	{
		var spawns = Scene.GetAllComponents<SpawnPoint>()
			.Where(sp => sp.Role == role)
			.ToList();

		if (spawns.Count == 0) return Rotation.Identity;
		return spawns[Random.Shared.Int(0, spawns.Count - 1)].WorldRotation;
	}
}