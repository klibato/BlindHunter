public enum GameState
{
	Playing,
	SurvivorsWon,
	KillerWon
}

/// <summary>Tracks overall game state and broadcasts victory conditions to all clients.</summary>
public sealed class GameStateManager : Component
{
	[Sync( SyncFlags.FromHost )] public GameState CurrentState { get; set; } = GameState.Playing;

	public static GameStateManager Instance { get; private set; }

	protected override void OnAwake()
	{
		Instance = this;
	}

	/// <summary>Declares survivors victorious. Host only.</summary>
	public void DeclareSurvivorsVictory()
	{
		if ( !Networking.IsHost ) return;
		if ( CurrentState != GameState.Playing ) return;

		CurrentState = GameState.SurvivorsWon;
		Log.Info( "Game over: Survivors won!" );
	}

	/// <summary>Declares the killer victorious. Host only.</summary>
	public void DeclareKillerVictory()
	{
		if ( !Networking.IsHost ) return;
		if ( CurrentState != GameState.Playing ) return;

		CurrentState = GameState.KillerWon;
		Log.Info( "Game over: Killer won!" );
	}

	/// <summary>Resets the game state to Playing for a new round. Host only.</summary>
	public void ResetToPlaying()
	{
		if ( !Networking.IsHost ) return;
		CurrentState = GameState.Playing;
		Log.Info( "GameState reset to Playing" );
	}
}
