/// <summary>Assigns Killer/Survivor role on spawn and emits positional noise when the player moves.</summary>
public sealed class PlayerSetup : Component
{
	private const float NoiseInterval = 1.2f;
	private const float MovementThreshold = 100f;

	[Sync(SyncFlags.FromHost)] public PlayerRole Role { get; set; } = PlayerRole.None;
	[Sync(SyncFlags.FromHost)] public bool IsAlive { get; set; } = true;
	[Sync] public Rotation EyeRotation { get; set; }

	private float _noiseTimer;
	private SkinnedModelRenderer _bodyRenderer;
	private PlayerController _controller;
	private CameraComponent _camera;

	protected override void OnStart()
	{
		_bodyRenderer = GetComponentInChildren<SkinnedModelRenderer>();
		_controller = GetComponent<PlayerController>();
		_camera = GetComponentInChildren<CameraComponent>();

		if (Networking.IsHost)
		{
			int killersCount = Scene.GetAllComponents<PlayerSetup>().Count(p => p.Role == PlayerRole.Killer);
			Role = killersCount == 0 ? PlayerRole.Killer : PlayerRole.Survivor;
			Log.Info($"Player '{GameObject.Name}' assigned role: {Role}");
		}

		if (IsProxy)
		{
			if (_camera != null)
				_camera.Enabled = false;
		}
	}

	protected override void OnUpdate()
	{
		if (!IsAlive)
		{
			HandleDeathState();
			return;
		}

		// Sync de la rotation de la tête (uniquement pour le joueur local)
		if (!IsProxy && _camera != null)
		{
			EyeRotation = _camera.WorldRotation;
		}

		ApplyRoleColor();
		HandleNoiseEmission();
	}

	private void HandleNoiseEmission()
	{
		if (!IsAlive) return;
		if (IsProxy) return;
		if (_controller == null) return;

		float speed = _controller.Velocity.Length;
		if (speed > MovementThreshold && _noiseTimer <= 0f)
		{
			EmitNoise(WorldPosition, speed);
			_noiseTimer = NoiseInterval;
		}

		_noiseTimer -= Time.Delta;
	}

	[Rpc.Broadcast]
	private void EmitNoise(Vector3 position, float intensity)
	{
		var localPlayer = Scene.GetAllComponents<PlayerSetup>().FirstOrDefault(p => !p.IsProxy);
		if (localPlayer == null) return;
		if (localPlayer.Role != PlayerRole.Killer) return;

		NoiseVisualizer.AddNoise(position, intensity);
	}

	private void ApplyRoleColor()
	{
		if (_bodyRenderer == null) return;

		_bodyRenderer.Tint = Role switch
		{
			PlayerRole.Killer => Color.Red,
			PlayerRole.Survivor => Color.Blue,
			_ => Color.White
		};
	}

	public void Kill()
	{
		if (!Networking.IsHost) return;
		if (!IsAlive) return;

		NoiseVisualizer.AddNoise(WorldPosition, 400f);
		IsAlive = false;
		Log.Info($"{GameObject.Name} was killed");

		CheckSurvivorsAllDead();
	}

	private void CheckSurvivorsAllDead()
	{
		var survivors = Scene.GetAllComponents<PlayerSetup>()
			.Where(p => p.Role == PlayerRole.Survivor);

		bool allDead = survivors.All(s => !s.IsAlive);
		bool atLeastOneSurvivor = survivors.Any();

		if (allDead && atLeastOneSurvivor)
		{
			GameStateManager.Instance?.DeclareKillerVictory();
		}
	}

	private void HandleDeathState()
	{
		if (_controller != null)
		{
			_controller.Enabled = false;
		}
	}
}