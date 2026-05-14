using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox;

public sealed class GameManager : Component, Component.INetworkListener
{
	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public Vector3 LobbySpawnPosition { get; set; } = new Vector3(0, 0, 100);
	[Property] public Vector3 FallbackSpawnPosition { get; set; } = new Vector3(0, 0, 50);
	[Property] public float StartCountdownDuration { get; set; } = 3f;
	[Property] public int MaxPlayers { get; set; } = 5;
	[Property] public int MinPlayers { get; set; } = 2;

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

		if (GetAllPlayers().Count >= MaxPlayers)
		{
			channel.Kick("Lobby is full");
			return;
		}

		var playerObject = PlayerPrefab.Clone(LobbySpawnPosition);
		playerObject.Name = $"Player - {channel.DisplayName}";

		var setup = playerObject.GetComponent<PlayerSetup>();
		if (setup != null)
		{
			setup.AssignedRole = PlayerRole.None;

			if (State == LobbyState.InGame)
			{
				setup.Role = PlayerRole.None;
				setup.IsAlive = false;
			}
		}

		playerObject.NetworkSpawn(channel);
		if (State != LobbyState.InGame);
	}

	void INetworkListener.OnDisconnected(Connection channel)
	{
		if (!Networking.IsHost) return;
		if (State != LobbyState.InGame) return;

		var player = Scene.GetAllComponents<PlayerSetup>()
			.FirstOrDefault(p => p.Network.Owner == channel);

		if (player == null) return;

		if (player.Role == PlayerRole.Killer)
		{
			Log.Info($"Killer ({channel.DisplayName}) disconnected — survivors win");
			GameStateManager.Instance?.DeclareSurvivorsVictory();
		}
		else if (player.Role == PlayerRole.Survivor)
		{
			var remainingSurvivors = Scene.GetAllComponents<PlayerSetup>()
				.Where(p => p.Network.Owner != channel && p.Role == PlayerRole.Survivor && p.IsAlive)
				.ToList();

			if (remainingSurvivors.Count == 0)
			{
				GameStateManager.Instance?.DeclareKillerVictory();
			}
		}
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

	public void RequestStartGame()
	{
		if (!Networking.IsHost) return;
		if (State != LobbyState.Lobby) return;

		var players = GetAllPlayers();
		if (players.Count < MinPlayers)
		{
			Log.Warning($"Need at least {MinPlayers} players to start (currently {players.Count})");
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

		int killerIndex = Random.Shared.Int(0, players.Count - 1);
		Log.Info($"Killer chosen: {players[killerIndex].GameObject.Name}");

		var killerSpawns = GetSpawnPoints(PlayerRole.Killer);
		var survivorSpawns = GetSpawnPoints(PlayerRole.Survivor);
		int killerUsed = 0, survivorUsed = 0;

		for (int i = 0; i < players.Count; i++)
		{
			var p = players[i];
			p.Role = (i == killerIndex) ? PlayerRole.Killer : PlayerRole.Survivor;
			p.AssignedRole = p.Role;
			p.IsAlive = true;

			Sandbox.SpawnPoint spawn;
			if (p.Role == PlayerRole.Killer)
				spawn = killerUsed < killerSpawns.Count ? killerSpawns[killerUsed++] : null;
			else
				spawn = survivorUsed < survivorSpawns.Count ? survivorSpawns[survivorUsed++] : null;

			Vector3 pos = spawn != null ? spawn.WorldPosition : LobbySpawnPosition;
			Rotation rot = spawn != null ? spawn.WorldRotation : Rotation.Identity;
			p.TeleportRpc(pos, rot);
			Log.Info($"{p.GameObject.Name} → {p.Role} → {(spawn != null ? spawn.GameObject.Name : "LOBBY_FALLBACK")} at {pos}");
		}

		Log.Info("Game started!");
	}
	
	/// <summary>
	/// Détruit et recrée les joueurs morts pour éviter l'état corrompu
	/// </summary>
	private void RespawnDeadPlayers()
	{
		if (!Networking.IsHost) return;
		
		var deadPlayers = Scene.GetAllComponents<PlayerSetup>()
			.Where(p => !p.IsAlive)
			.ToList();
		
		foreach (var deadPlayer in deadPlayers)
		{
			var owner = deadPlayer.Network.Owner;
			if (owner == null) continue;

			var oldRole = deadPlayer.Role;
			Log.Info($"Respawnning {owner.DisplayName} (was {oldRole}) at lobby");

			deadPlayer.GameObject.Destroy();

			var newPlayer = PlayerPrefab.Clone(LobbySpawnPosition);
			newPlayer.Name = $"Player - {owner.DisplayName}";

			var newSetup = newPlayer.GetComponent<PlayerSetup>();
			if (newSetup != null)
			{
				newSetup.Role = PlayerRole.None;
				newSetup.AssignedRole = PlayerRole.None;
				newSetup.IsAlive = true;
			}

			newPlayer.NetworkSpawn(owner);
		}
	}
	
	/// <summary>
	/// Reset le lobby et recrée les joueurs morts
	/// </summary>
	public void RequestReturnToLobby()
	{
		if (!Networking.IsHost) return;
		if (State != LobbyState.InGame) return;

		Log.Info("Returning to lobby...");

		State = LobbyState.Lobby;
		CountdownTimer = 0f;
		GameStateManager.Instance?.ResetToPlaying();

		// 🔥 RECRÉE LES JOUEURS MORTS
		RespawnDeadPlayers();
		
		// Téléporte tous les joueurs (vivants et ressuscités) au lobby et reset leur rôle
		// pour que le banner ne se déclenche qu'au prochain démarrage de partie
		var players = GetAllPlayers();
		foreach (var p in players)
		{
			p.Role = PlayerRole.None;
			p.AssignedRole = PlayerRole.None;
			p.TeleportRpc(LobbySpawnPosition, Rotation.Identity);
		}

		// Reset toutes les quêtes
		foreach (var interactable in Scene.GetAllComponents<Interactable>())
		{
			interactable.IsCompleted = false;
		}
		foreach (var group in Scene.GetAllComponents<QuestGroup>())
		{
			group.IsCompleted = false;
		}
		if (QuestManager.Instance != null)
		{
			QuestManager.Instance.CompletedQuests = 0;
		}

		// Cleanup des stones / objets jetés
		var trackers = Scene.GetAllComponents<ThrowableTracker>().ToList();
		foreach (var t in trackers)
			t.GameObject.Destroy();

		// Cleanup des cadavres ragdoll restants sur tous les clients
		CleanupCorpsesRpc();

		// Respawn les pickables désactivés
		foreach (var pickable in Pickable.All)
		{
			if (pickable.IsValid())
				pickable.ResetPickable();
		}

		Log.Info("Back to lobby.");
	}
	
	[Rpc.Broadcast]
	private void CleanupCorpsesRpc()
	{
		foreach (var corpse in PlayerSetup.AllCorpses)
		{
			if (corpse.IsValid())
				corpse.Destroy();
		}
		PlayerSetup.AllCorpses.Clear();
	}

	public List<PlayerSetup> GetAllPlayers()
	{
		return Scene.GetAllComponents<PlayerSetup>().ToList();
	}

	private List<Sandbox.SpawnPoint> GetSpawnPoints(PlayerRole role)
	{
		string nameFilter = role == PlayerRole.Killer ? "Killer" : "Survivor";
		return Scene.GetAllComponents<Sandbox.SpawnPoint>()
			.Where(sp => sp.GameObject.Name.Contains(nameFilter))
			.ToList();
	}
}