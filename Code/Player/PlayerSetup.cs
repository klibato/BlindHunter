/// <summary>Assigns Killer/Survivor role on spawn and emits positional noise when the player moves.</summary>
public sealed class PlayerSetup : Component
{
	private const float NoiseInterval = 1.2f;
	private const float MovementThreshold = 100f;

	[Sync(SyncFlags.FromHost)] public PlayerRole Role { get; set; } = PlayerRole.None;
	[Sync(SyncFlags.FromHost)] public bool IsAlive { get; set; } = true;
	[Sync] public Rotation EyeRotation { get; set; }
	public PlayerRole AssignedRole { get; set; } = PlayerRole.None;
	private float _noiseTimer;
	private SkinnedModelRenderer _bodyRenderer;
	private PlayerController _controller;
	private CameraComponent _camera;

	protected override void OnStart()
	{
		_bodyRenderer = GetComponentInChildren<SkinnedModelRenderer>();
		_controller = GetComponent<PlayerController>();
		_camera = GetComponentInChildren<CameraComponent>();

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
		// Désactive le PlayerInteractor (le joueur mort ne peut plus interagir)
		var interactor = GameObject.GetComponent<PlayerInteractor>();
		if (interactor != null) interactor.Enabled = false;

		// Désactive l'inventaire
		var inventory = GameObject.GetComponent<PlayerInventory>();
		if (inventory != null) inventory.Enabled = false;

		// Désactive la flashlight
		var flashlight = GameObject.GetComponentInChildren<Flashlight>();
		if (flashlight != null) flashlight.Enabled = false;
	}
}