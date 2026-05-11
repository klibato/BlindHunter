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
			Log.Info($"{channel.DisplayName} tried to join but lobby is full ({MaxPlayers})");
			channel.Kick( "Lobby is full" );
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
				// Rejoint en cours de partie → spectateur direct
				setup.Role = PlayerRole.None;
				setup.IsAlive = false;
				Log.Info($"{channel.DisplayName} joined as spectator (game in progress)");
			}
		}

		playerObject.NetworkSpawn(channel);
		if (State != LobbyState.InGame)
			Log.Info($"{channel.DisplayName} joined the lobby");
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
			// Vérifie s'il reste des survivors vivants (hors celui qui part)
			var remainingSurvivors = Scene.GetAllComponents<PlayerSetup>()
				.Where(p => p.Network.Owner != channel && p.Role == PlayerRole.Survivor && p.IsAlive)
				.ToList();

			if (remainingSurvivors.Count == 0)
			{
				Log.Info($"Last survivor ({channel.DisplayName}) disconnected — killer wins");
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

	private async void ActuallyStartGame()
	{
		State = LobbyState.InGame;

		var players = GetAllPlayers();
		if (players.Count == 0) return;

		int killerIndex = Random.Shared.Int(0, players.Count - 1);
		Log.Info($"Killer chosen: {players[killerIndex].GameObject.Name}");

		for (int i = 0; i < players.Count; i++)
		{
			var p = players[i];
			p.Role = (i == killerIndex) ? PlayerRole.Killer : PlayerRole.Survivor;
			p.AssignedRole = p.Role;
		}

		await Task.DelayRealtimeSeconds(0.15f);

		var killerSpawns = GetShuffledSpawnPoints(PlayerRole.Killer);
		var survivorSpawns = GetShuffledSpawnPoints(PlayerRole.Survivor);
		int killerUsed = 0, survivorUsed = 0;

		for (int i = 0; i < players.Count; i++)
		{
			var p = players[i];
			Sandbox.SpawnPoint spawn = null;

			if (p.Role == PlayerRole.Killer && killerUsed < killerSpawns.Count)
				spawn = killerSpawns[killerUsed++];
			else if (p.Role == PlayerRole.Survivor && survivorUsed < survivorSpawns.Count)
				spawn = survivorSpawns[survivorUsed++];

			Vector3 spawnPos = spawn != null ? spawn.WorldPosition : FallbackSpawnPosition;
			Rotation spawnRot = spawn != null ? spawn.WorldRotation : Rotation.Identity;
			p.TeleportRpc(spawnPos, spawnRot);
		}

		Log.Info("Game started!");
	}
	/// <summary>
	/// Appelé quand le host clique "Return to Lobby" sur l'écran de fin.
	/// Reset tout l'état et téléporte les joueurs au lobby.
	/// </summary>
	public async void RequestReturnToLobby()
	{
		if (!Networking.IsHost) return;
		if (State != LobbyState.InGame) return;

		Log.Info("Returning to lobby...");

		State = LobbyState.Lobby;
		CountdownTimer = 0f;
		GameStateManager.Instance?.ResetToPlaying();

		var players = GetAllPlayers();
		foreach (var p in players)
			p.ResetForLobby();

		// Attend que IsAlive/Role sync arrivent sur les clients avant de teleporter
		await Task.DelayRealtimeSeconds(0.15f);

		foreach (var p in players)
			p.TeleportRpc(LobbySpawnPosition, Rotation.Identity);

		// 3. Reset toutes les quêtes
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

		// 4. Cleanup des stones / objets jetés (tout objet avec ThrowableTracker)
		var trackers = Scene.GetAllComponents<ThrowableTracker>().ToList();
		foreach (var t in trackers)
			t.GameObject.Destroy();

		// 5. Respawn les pickables désactivés (keycard, etc.)
		foreach (var pickable in Pickable.All)
		{
			if (pickable.IsValid())
				pickable.ResetPickable();
		}

		Log.Info("Back to lobby.");
	}
public List<PlayerSetup> GetAllPlayers()
	{
		return Scene.GetAllComponents<PlayerSetup>().ToList();
	}

	private List<Sandbox.SpawnPoint> GetShuffledSpawnPoints(PlayerRole role)
	{
		string nameFilter = role == PlayerRole.Killer ? "Killer" : "Survivor";
		var spawns = Scene.GetAllComponents<Sandbox.SpawnPoint>()
			.Where(sp => sp.GameObject.Name.Contains(nameFilter))
			.ToList();

		// Fisher-Yates shuffle
		for (int i = spawns.Count - 1; i > 0; i--)
		{
			int j = Random.Shared.Int(0, i);
			(spawns[i], spawns[j]) = (spawns[j], spawns[i]);
		}

		return spawns;
	}
}