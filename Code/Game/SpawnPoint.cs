/// <summary>
/// Marque un emplacement de spawn dans la map. Utilisé par GameManager pour positionner les joueurs.
/// </summary>
public sealed class SpawnPoint : Component
{
	[Property] public PlayerRole Role { get; set; } = PlayerRole.Survivor;

	protected override void OnUpdate()
	{
		if (Role == PlayerRole.Killer)
		{
			Gizmo.Draw.Color = Color.Red;
		}
		else
		{
			Gizmo.Draw.Color = Color.Blue;
		}
		Gizmo.Draw.LineSphere(WorldPosition, 30f);
		Gizmo.Draw.Line(WorldPosition, WorldPosition + WorldRotation.Forward * 50f);
	}
}