/// <summary>Assigns Killer/Survivor role on spawn and emits positional noise when the player moves.</summary>
public sealed class PlayerSetup : Component
{
	private const float NoiseInterval = 0.5f;
	private const float MovementThreshold = 50f;

	[Sync( SyncFlags.FromHost )] public PlayerRole Role { get; set; } = PlayerRole.None;

	private float _noiseTimer;
	private SkinnedModelRenderer _bodyRenderer;
	private PlayerController _controller;

	protected override void OnStart()
	{
		_bodyRenderer = GetComponentInChildren<SkinnedModelRenderer>();
		_controller = GetComponent<PlayerController>();

		if ( Networking.IsHost )
		{
			int killersCount = Scene.GetAllComponents<PlayerSetup>().Count( p => p.Role == PlayerRole.Killer );
			Role = killersCount == 0 ? PlayerRole.Killer : PlayerRole.Survivor;
			Log.Info( $"Player '{GameObject.Name}' assigned role: {Role}" );
		}

		if ( IsProxy )
		{
			var camera = GetComponentInChildren<CameraComponent>();
			if ( camera != null )
				camera.Enabled = false;
		}
	}

	protected override void OnUpdate()
	{
		ApplyRoleColor();
		HandleNoiseEmission();
	}

	private void HandleNoiseEmission()
	{
		if ( IsProxy ) return;
		if ( _controller == null ) return;

		float speed = _controller.Velocity.Length;
		if ( speed > MovementThreshold && _noiseTimer <= 0f )
		{
			EmitNoise( WorldPosition, speed );
			_noiseTimer = NoiseInterval;
		}

		_noiseTimer -= Time.Delta;
	}

	[Rpc.Broadcast]
	private void EmitNoise( Vector3 position, float intensity )
	{
		var localPlayer = Scene.GetAllComponents<PlayerSetup>().FirstOrDefault( p => !p.IsProxy );
		if ( localPlayer == null ) return;
		if ( localPlayer.Role != PlayerRole.Killer ) return;

		NoiseVisualizer.AddNoise( position, intensity );
	}

	private void ApplyRoleColor()
	{
		if ( _bodyRenderer == null ) return;

		_bodyRenderer.Tint = Role switch
		{
			PlayerRole.Killer => Color.Red,
			PlayerRole.Survivor => Color.Blue,
			_ => Color.White
		};
	}
}
