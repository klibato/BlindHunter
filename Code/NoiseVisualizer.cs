using Sandbox;
using System.Collections.Generic;

public sealed class NoiseVisualizer : Component
{
	private struct NoiseEntry
	{
		public Vector3 Position;
		public float Intensity;
		public float SpawnedAt;
		public float Lifetime;
	}

	private static List<NoiseEntry> _noises = new();

	public static void AddNoise( Vector3 position, float intensity )
	{
		_noises.Add( new NoiseEntry
		{
			Position = position,
			Intensity = intensity,
			SpawnedAt = RealTime.Now,
			Lifetime = 2f
		} );
	}

	protected override void OnUpdate()
	{
		// Nettoie les expirés
		_noises.RemoveAll( n => RealTime.Now - n.SpawnedAt >= n.Lifetime );

		// Dessine
		foreach ( var noise in _noises )
		{
			float age = RealTime.Now - noise.SpawnedAt;
			float t = age / noise.Lifetime;
			float alpha = 1f - t;

			// Rayon qui s'agrandit avec le temps
			float currentRadius = (noise.Intensity / 100f) * t * 100f;

			// Couleur blanche qui fade out
			Gizmo.Draw.Color = Color.White.WithAlpha( alpha );
			Gizmo.Draw.LineSphere( noise.Position, currentRadius );
		}
	}
}