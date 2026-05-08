/// <summary>
/// Gère la visibilité de la souris en fonction de l'état de jeu.
/// Souris visible en Lobby ou quand la game est terminée (écran de fin).
/// </summary>
public sealed class MouseVisibility : Component
{
	protected override void OnUpdate()
	{
		if ( GameManager.Instance == null ) return;

		bool inLobby = GameManager.Instance.State == LobbyState.Lobby;
		bool gameOver = GameStateManager.Instance != null
			&& GameStateManager.Instance.CurrentState != GameState.Playing;

		Mouse.Visible = inLobby || gameOver;
	}
}