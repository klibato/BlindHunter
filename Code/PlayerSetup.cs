using Sandbox;
using System.Linq;

public sealed class PlayerSetup : Component
{
	[Sync(SyncFlags.FromHost)] public PlayerRole Role { get; set; } = PlayerRole.None;
	protected override void OnStart()
	{
		// Le host attribue le rôle
		if (Networking.IsHost)
		{
			int killersCount = Scene.GetAllComponents<PlayerSetup>().Count(p => p.Role == PlayerRole.Killer);
			Role = killersCount == 0 ? PlayerRole.Killer : PlayerRole.Survivor;
			Log.Info($"Host assigned role: {Role} (killers count was {killersCount})");
		}

		if (IsProxy)
		{
			var camera = GetComponentInChildren<CameraComponent>();
			if (camera != null)
				camera.Enabled = false;
		}
	}

	protected override void OnUpdate()
	{
		ApplyRoleColor();
	}

	private void ApplyRoleColor()
	{
		var renderer = GetComponentInChildren<SkinnedModelRenderer>();
		if (renderer == null)
			return;

		Color targetColor = Role switch
		{
			PlayerRole.Killer => Color.Red,
			PlayerRole.Survivor => Color.Blue,
			_ => Color.White
		};

		renderer.Tint = targetColor;
	}
}