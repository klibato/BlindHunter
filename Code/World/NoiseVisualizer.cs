/// <summary>Tracks active noise events for the Killer's vision shader. No visual artifacts in world space.</summary>
public sealed class NoiseVisualizer : Component
{
	private const float NoiseLifetime = 2.5f;

	public class NoiseEntry
	{
		public Vector3 Position;
		public float Intensity;
		public float SpawnedAt;
		public float Lifetime;
	}

	private static List<NoiseEntry> _noises = new();
	private static NoiseVisualizer _instance;

	protected override void OnAwake()
	{
		_instance = this;
		_noises.Clear();
	}

	/// <summary>Registers a noise event at the given world position.</summary>
	public static void AddNoise( Vector3 position, float intensity )
	{
		if ( _instance == null ) return;

		_noises.Add( new NoiseEntry
		{
			Position = position,
			Intensity = intensity,
			SpawnedAt = RealTime.Now,
			Lifetime = NoiseLifetime,
		} );
	}

	/// <summary>Returns the list of currently active noises (used by KillerVisionEffect shader).</summary>
	public static IReadOnlyList<NoiseEntry> GetActiveNoises()
	{
		return _noises;
	}

	protected override void OnUpdate()
	{
		// Cleanup des entrées expirées
		for ( int i = _noises.Count - 1; i >= 0; i-- )
		{
			var entry = _noises[i];
			float age = RealTime.Now - entry.SpawnedAt;

			if ( age >= entry.Lifetime )
			{
				_noises.RemoveAt( i );
			}
		}
	}
}