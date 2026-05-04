using Sandbox.Rendering;

[Title("KillerVisionEffect")]
[Category("Post Processing")]
public sealed class KillerVisionEffect : BasePostProcess<KillerVisionEffect>
{
	[Property] public PlayerSetup TargetPlayer { get; set; }

	public override void Render()
	{
		// FILTRE DÉSACTIVÉ POUR DEBUG
		// if (TargetPlayer == null) return;
		// if (TargetPlayer.Role != PlayerRole.Killer) return;

		var shader = Material.FromShader("shaders/pp_test_white.shader");
		if (shader == null) return;

		var blit = BlitMode.WithBackbuffer(shader, Stage.AfterPostProcess, 200, false);
		Blit(blit, "KillerVisionEffect");
	}
}