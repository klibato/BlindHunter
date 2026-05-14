using System.Linq;
using Sandbox;
using Sandbox.Audio;

/// <summary>
/// Settings utilisateur persistés dans FileSystem.Data ("settings.json").
/// Chaque setter sauvegarde immédiatement et propage la valeur au mixer correspondant
/// (Master / Game / Music). Load au premier accès via le static ctor.
/// Les .sound doivent être assignés au bon DefaultMixer dans l'inspector pour que
/// le scaling agisse (Game pour les SFX, Music pour la musique).
/// </summary>
public static class GameSettings
{
	private const string FileName = "settings.json";

	private class State
	{
		public float MasterVolume { get; set; } = 1.0f;
		public float SfxVolume { get; set; } = 1.0f;
		public float MusicVolume { get; set; } = 1.0f;
		public bool Fullscreen { get; set; } = true;
		public Language Language { get; set; } = Language.English;
	}

	private static State _state = new State();

	public static float MasterVolume
	{
		get => _state.MasterVolume;
		set { _state.MasterVolume = value; ApplyToMixers(); Save(); }
	}

	public static float SfxVolume
	{
		get => _state.SfxVolume;
		set { _state.SfxVolume = value; ApplyToMixers(); Save(); }
	}

	public static float MusicVolume
	{
		get => _state.MusicVolume;
		set { _state.MusicVolume = value; ApplyToMixers(); Save(); }
	}

	public static bool Fullscreen
	{
		get => _state.Fullscreen;
		set { _state.Fullscreen = value; Save(); }
	}

	public static Language Language
	{
		get => _state.Language;
		set { _state.Language = value; Lang.SetLanguage( value ); Save(); }
	}

	/// <summary>
	/// Pousse les valeurs vers les mixers natifs s&box. À appeler au boot une fois
	/// que les mixers sont initialisés (cf AudioMixerInitializer) et à chaque changement.
	/// </summary>
	public static void ApplyToMixers()
	{
		try
		{
			var master = Mixer.Master;
			if ( master != null )
			{
				master.Volume = _state.MasterVolume;

				var game = Mixer.FindMixerByName( "Game" );
				if ( game != null ) game.Volume = _state.SfxVolume;

				var music = Mixer.FindMixerByName( "Music" );
				if ( music != null ) music.Volume = _state.MusicVolume;
			}
		}
		catch ( System.Exception e )
		{
			Log.Warning( $"GameSettings ApplyToMixers failed: {e.Message}" );
		}
	}

	public static void Load()
	{
		try
		{
			if ( FileSystem.Data.FileExists( FileName ) )
			{
				_state = FileSystem.Data.ReadJson<State>( FileName ) ?? new State();
				Lang.SetLanguage( _state.Language );
				return;
			}
		}
		catch ( System.Exception e )
		{
			Log.Warning( $"GameSettings load failed: {e.Message} — using defaults" );
		}
		_state = new State();
		Lang.SetLanguage( _state.Language );
	}

	private static void Save()
	{
		try
		{
			FileSystem.Data.WriteJson( FileName, _state );
		}
		catch ( System.Exception e )
		{
			Log.Warning( $"GameSettings save failed: {e.Message}" );
		}
	}
}
