using Sandbox;
using System.Collections.Generic;

public sealed class NoiseVisualizer : Component
{
	private struct NoiseEntry
	{
		public Vector3 Position;
		public float Intensity;
		public float ExpiresAt;
	}

	private static List<NoiseEntry> _noises = new();

	public static void AddNoise( Vector3 position, float intensity )
	{
		_noises.Add( new NoiseEntry
		{
			Position = position,
			Intensity = intensity,
			ExpiresAt = RealTime.Now + 2f
		} );
	}

	protected override void OnUpdate()
	{
		// Nettoie les bruits expirés
		_noises.RemoveAll( n => n.ExpiresAt < RealTime.Now );

		// Dessine les bruits actifs
		foreach ( var noise in _noises )
		{
			float lifeLeft = noise.ExpiresAt - RealTime.Now;
			float alpha = lifeLeft / 2f; // fade out

			// Rayon proportionnel à l'intensité
			float radius = noise.Intensity / 2f;

			Gizmo.Draw.Color = Color.Yellow.WithAlpha( alpha );
			Gizmo.Draw.LineSphere( noise.Position, radius );
		}
	}
}