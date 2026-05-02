using Sandbox;

public enum GameState
{
	Playing,
	SurvivorsWon,
	KillerWon
}

public sealed class GameStateManager : Component
{
	[Sync(SyncFlags.FromHost)] public GameState CurrentState { get; set; } = GameState.Playing;

	public static GameStateManager Instance { get; private set; }

	protected override void OnAwake()
	{
		Instance = this;
	}

	public void DeclareSurvivorsVictory()
	{
		if ( !Networking.IsHost ) return;
		if ( CurrentState != GameState.Playing ) return;

		CurrentState = GameState.SurvivorsWon;
		Log.Info( "Game over: Survivors won!" );
	}

	public void DeclareKillerVictory()
	{
		if ( !Networking.IsHost ) return;
		if ( CurrentState != GameState.Playing ) return;

		CurrentState = GameState.KillerWon;
		Log.Info( "Game over: Killer won!" );
	}
}