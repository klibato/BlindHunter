using Sandbox.Rendering;
using System.Linq;

[Title("KillerVisionEffect")]
[Category("Post Processing")]
public sealed class KillerVisionEffect : BasePostProcess<KillerVisionEffect>
{
	[Property] public PlayerSetup TargetPlayer { get; set; }
	[Property] public float WaveSpeedFactor { get; set; } = 5.0f;
	[Property] public float WaveThickness { get; set; } = 150f;

	private const int MaxSources = 8;

	public override void Render()
	{
		if (TargetPlayer == null) return;
		if (TargetPlayer.Role != PlayerRole.Killer) return;

		var shader = Material.FromShader("shaders/pp_edge_detect.shader");
		if (shader == null) return;

		var noises = NoiseVisualizer.GetActiveNoises();
		var activeList = noises.Take(MaxSources).ToList();
		float now = RealTime.Now;

		// Initialise toutes les sources avec une "source vide" (intensity = 0)
		for (int i = 0; i < MaxSources; i++)
		{
			Vector4 sourceData = new Vector4(0, 0, 0, 0); // xyz=pos, w=intensity (0 = inactif)
			Vector4 timingData = new Vector4(999, 1, 0, 0); // x=age (énorme = inactif), y=lifetime

			if (i < activeList.Count)
			{
				var noise = activeList[i];
				sourceData = new Vector4(noise.Position.x, noise.Position.y, noise.Position.z, noise.Intensity);
				timingData = new Vector4(now - noise.SpawnedAt, noise.Lifetime, 0, 0);
			}

			Attributes.Set($"g_Source{i}", sourceData);
			Attributes.Set($"g_Timing{i}", timingData);
		}

		Attributes.Set("g_WaveSpeedFactor", WaveSpeedFactor);
		Attributes.Set("g_WaveThickness", WaveThickness);

		var blit = BlitMode.WithBackbuffer(shader, Stage.AfterPostProcess, 200, false);
		Blit(blit, "KillerVisionEffect");
	}
}