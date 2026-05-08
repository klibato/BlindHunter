/// <summary>
/// Marque un emplacement de spawn dans la map. Utilisé par GameManager pour positionner les joueurs.
/// </summary>
public sealed class SpawnPoint : Component
{
	[Property] public PlayerRole Role { get; set; } = PlayerRole.Survivor;

	protected override void OnUpdate()
	{

	}
}