using System;
using System.Linq;
using System.Threading.Tasks;

/// <summary>Manages lobby creation and player spawning when a connection becomes active.</summary>
public sealed class GameManager : Component, Component.INetworkListener
{
	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public Vector3 FallbackSpawnPosition { get; set; } = new Vector3( 0, 0, 50 );

	private int _killerCount = 0;

	protected override async Task OnLoad()
	{
		if ( Scene.IsEditor )
			return;

		if ( !Networking.IsActive )
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds( 0.1f );
			Networking.CreateLobby();
		}
	}

	void INetworkListener.OnActive( Connection channel )
	{
		if ( !Networking.IsHost )
			return;

		if ( PlayerPrefab is null )
		{
			Log.Warning( "PlayerPrefab is not set on GameManager!" );
			return;
		}

		// Détermine le rôle du joueur (premier connecté = killer, suivants = survivors)
		PlayerRole assignedRole = ( _killerCount == 0 ) ? PlayerRole.Killer : PlayerRole.Survivor;
		if ( assignedRole == PlayerRole.Killer )
		{
			_killerCount++;
		}

		// Trouve un spawn point libre pour ce rôle
		Vector3 spawnPos = FindSpawnPosition( assignedRole );
		Rotation spawnRot = FindSpawnRotation( assignedRole );

		var playerObject = PlayerPrefab.Clone( spawnPos, spawnRot );
		playerObject.Name = $"Player - {channel.DisplayName}";
		playerObject.NetworkSpawn( channel );
	}

	private Vector3 FindSpawnPosition( PlayerRole role )
	{
		var spawns = Scene.GetAllComponents<SpawnPoint>()
			.Where( sp => sp.Role == role )
			.ToList();

		if ( spawns.Count == 0 )
		{
			Log.Warning( $"No SpawnPoint found for role {role}, using fallback position" );
			return FallbackSpawnPosition;
		}

		// Choisit aléatoirement un spawn parmi ceux du bon rôle
		var chosen = spawns[Random.Shared.Int( 0, spawns.Count - 1 )];
		return chosen.WorldPosition;
	}

	private Rotation FindSpawnRotation( PlayerRole role )
	{
		var spawns = Scene.GetAllComponents<SpawnPoint>()
			.Where( sp => sp.Role == role )
			.ToList();

		if ( spawns.Count == 0 )
		{
			return Rotation.Identity;
		}

		var chosen = spawns[Random.Shared.Int( 0, spawns.Count - 1 )];
		return chosen.WorldRotation;
	}
}