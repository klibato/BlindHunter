/// <summary>
/// Gère la visibilité de la souris en fonction de l'état de jeu.
/// Souris visible en Lobby, cachée en Starting/InGame.
/// </summary>
public sealed class MouseVisibility : Component
{
	protected override void OnUpdate()
	{
		if ( GameManager.Instance == null ) return;

		bool shouldShowMouse = GameManager.Instance.State == LobbyState.Lobby;
		Mouse.Visible = shouldShowMouse;
	}
}