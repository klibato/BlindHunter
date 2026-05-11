/// <summary>
/// Pousse les volumes persistés (GameSettings) aux mixers natifs s&box au boot
/// de la scène. À placer sur un GameObject persistent (ex: GameManager root) pour
/// que les valeurs sauvegardées soient réappliquées au démarrage du jeu.
/// </summary>
public sealed class AudioMixerInitializer : Component
{
	protected override void OnAwake()
	{
		GameSettings.Load();
		GameSettings.ApplyToMixers();
	}
}
