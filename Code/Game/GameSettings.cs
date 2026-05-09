/// <summary>
/// Settings utilisateur in-memory. Pas de persistence pour V1.0,
/// les valeurs reset au redémarrage du jeu.
/// V1.1 : ajouter persistence (FileSystem.Data) + SoundHelper qui propage MasterVolume aux Sound.Play() existants.
/// </summary>
public static class GameSettings
{
	public static float MasterVolume { get; set; } = 1.0f;
	public static float SfxVolume { get; set; } = 1.0f;
	public static float MusicVolume { get; set; } = 1.0f;
	public static bool Fullscreen { get; set; } = true;
}
