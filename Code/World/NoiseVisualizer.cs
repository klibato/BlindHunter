/// <summary>Renders temporary noise indicators visible only to the Killer when survivors move.</summary>
public sealed class NoiseVisualizer : Component
{
	private const float NoiseLifetime = 1.5f;
	private const float SphereExpansionFactor = 0.5f;

	private class NoiseEntry
	{
		public Vector3 Position;
		public float Intensity;
		public float SpawnedAt;
		public float Lifetime;
		public GameObject LightObject;
	}

	private static List<NoiseEntry> _noises = new();
	private static NoiseVisualizer _instance;

	protected override void OnAwake()
	{
		_instance = this;
		_noises.Clear();
	}

	/// <summary>Registers a noise event at the given world position. Visible to the local Killer only.</summary>
	public static void AddNoise( Vector3 position, float intensity )
	{
		if ( _instance == null ) return;

		var lightObject = _instance.Scene.CreateObject();
		lightObject.WorldPosition = position;
		lightObject.Name = "NoiseLight";

		var light = lightObject.Components.Create<PointLight>();
		light.LightColor = Color.White;
		light.Radius = intensity;

		_noises.Add( new NoiseEntry
		{
			Position = position,
			Intensity = intensity,
			SpawnedAt = RealTime.Now,
			Lifetime = NoiseLifetime,
			LightObject = lightObject
		} );
	}

	protected override void OnUpdate()
	{
		for ( int i = _noises.Count - 1; i >= 0; i-- )
		{
			var entry = _noises[i];
			float age = RealTime.Now - entry.SpawnedAt;
			float t = age / entry.Lifetime;

			if ( t >= 1f )
			{
				entry.LightObject?.Destroy();
				_noises.RemoveAt( i );
				continue;
			}

			float alpha = 1f - t;

			if ( entry.LightObject != null && entry.LightObject.IsValid() )
			{
				var light = entry.LightObject.GetComponent<PointLight>();
				if ( light != null )
					light.LightColor = Color.White.WithAlpha( alpha );
			}

			float currentRadius = entry.Intensity * t * SphereExpansionFactor;
			Gizmo.Draw.Color = Color.White.WithAlpha( alpha );
			Gizmo.Draw.LineSphere( entry.Position, currentRadius );
		}
	}
}
