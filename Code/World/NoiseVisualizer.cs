using Sandbox;
using System.Collections.Generic;

public sealed class NoiseVisualizer : Component
{
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
	}

	public static void AddNoise( Vector3 position, float intensity )
	{
		if ( _instance == null ) return;

		// Créer un GameObject avec une PointLight pour révéler la géométrie
		var lightObject = _instance.Scene.CreateObject();
		lightObject.WorldPosition = position;
		lightObject.Name = "NoiseLight";

		var light = lightObject.Components.Create<PointLight>();
		light.LightColor = Color.White;
		light.Radius = intensity; // rayon de révélation proportionnel à l'intensité
		// Brightness peut s'appeler différemment dans ta version, on adapte si besoin

		_noises.Add( new NoiseEntry
		{
			Position = position,
			Intensity = intensity,
			SpawnedAt = RealTime.Now,
			Lifetime = 1.5f,
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
				// Détruire la lumière et retirer
				entry.LightObject?.Destroy();
				_noises.RemoveAt( i );
				continue;
			}

			float alpha = 1f - t;

			// Animer l'intensité de la lumière (fade out)
			if ( entry.LightObject != null && entry.LightObject.IsValid() )
			{
				var light = entry.LightObject.GetComponent<PointLight>();
				if ( light != null )
				{
					light.LightColor = Color.White.WithAlpha( alpha );
				}
			}

			// Garder le wireframe Gizmo aussi pour le repère visuel
			float currentRadius = entry.Intensity * t * 0.5f;
			Gizmo.Draw.Color = Color.White.WithAlpha( alpha );
			Gizmo.Draw.LineSphere( entry.Position, currentRadius );
		}
	}
}