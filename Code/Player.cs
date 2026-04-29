using Sandbox;

public sealed class PlayerSetup : Component{
	protected override void OnStart()
	{
		Log.Info( $"Player.OnStart - IsProxy: {IsProxy}, GameObject: {GameObject.Name}" );

		if ( IsProxy )
		{
			var camera = GetComponentInChildren<CameraComponent>();
			if ( camera != null )
				camera.Enabled = false;
		}
	}
}