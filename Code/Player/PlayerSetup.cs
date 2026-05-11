/// <summary>Assigns Killer/Survivor role on spawn and emits positional noise when the player moves.</summary>
public sealed class PlayerSetup : Component
{
	private const float NoiseInterval = 1.2f;
	private const float MovementThreshold = 100f;

	[Sync(SyncFlags.FromHost)] public PlayerRole Role { get; set; } = PlayerRole.None;
	[Sync(SyncFlags.FromHost)] public bool IsAlive { get; set; } = true;
	[Sync] public Rotation EyeRotation { get; set; }
	public PlayerRole AssignedRole { get; set; } = PlayerRole.None;

	[Property] public SoundEvent DeathSound { get; set; }
	private float _noiseTimer;
	private SkinnedModelRenderer _bodyRenderer;
	private PlayerController _controller;
	private CameraComponent _camera;
	private GameObject _ragdoll;

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
		bool gameOver = GameStateManager.Instance != null
			&& GameStateManager.Instance.CurrentState != GameState.Playing;

		if (gameOver)
		{
			return;
		}

		if (!IsAlive)
		{
			HandleDeathState();
			return;
		}

		// Si on était mort et qu'on revit (reset lobby), réactive les composants côté local
		if (_controller != null && !_controller.Enabled) _controller.Enabled = true;
		var interactor = GameObject.GetComponent<PlayerInteractor>();
		if (interactor != null && !interactor.Enabled) interactor.Enabled = true;
		var inventory = GameObject.GetComponent<PlayerInventory>();
		if (inventory != null && !inventory.Enabled) inventory.Enabled = true;
		var flashlight = GameObject.GetComponentInChildren<Flashlight>();
		if (flashlight != null && !flashlight.Enabled) flashlight.Enabled = true;

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
		PlayDeathSoundRpc(WorldPosition);
		SpawnRagdollRpc();
		IsAlive = false;
		Log.Info($"{GameObject.Name} was killed");

		CheckSurvivorsAllDead();
	}

	[Rpc.Broadcast]
	private void PlayDeathSoundRpc(Vector3 position)
	{
		if (DeathSound != null) Sound.Play(DeathSound, position);
	}

	[Rpc.Broadcast]
	private void SpawnRagdollRpc()
	{
		if (_bodyRenderer == null) return;

		// Désactive AVANT le spawn du ragdoll : sinon le Rigidbody/colliders du player
		// (encore actifs sur le client owner ce frame) bloquent le corps en l'air
		if (_controller != null) _controller.Enabled = false;
		foreach (var rb in GameObject.GetComponentsInChildren<Rigidbody>())
		{
			rb.Enabled = false;
		}
		foreach (var collider in GameObject.GetComponentsInChildren<Collider>())
		{
			collider.Enabled = false;
		}

		var corpse = new GameObject(true, $"{GameObject.Name}_corpse");
		corpse.WorldPosition = _bodyRenderer.WorldPosition;
		corpse.WorldRotation = _bodyRenderer.WorldRotation;

		var renderer = corpse.AddComponent<SkinnedModelRenderer>();
		renderer.Model = _bodyRenderer.Model;
		renderer.Tint = _bodyRenderer.Tint;
		renderer.UseAnimGraph = false;

		var physics = corpse.AddComponent<ModelPhysics>();
		physics.Model = _bodyRenderer.Model;
		physics.Renderer = renderer;
		physics.MotionEnabled = true;

		// Cache le corps debout pour pas voir 2 modèles superposés
		_bodyRenderer.Enabled = false;

		_ragdoll = corpse;
	}

	[Rpc.Broadcast]
	private void CleanupRagdollRpc()
	{
		if (_ragdoll != null)
		{
			_ragdoll.Destroy();
			_ragdoll = null;
		}
		if (_bodyRenderer != null) _bodyRenderer.Enabled = true;
		foreach (var rb in GameObject.GetComponentsInChildren<Rigidbody>())
		{
			rb.Enabled = true;
		}
		foreach (var collider in GameObject.GetComponentsInChildren<Collider>())
		{
			collider.Enabled = true;
		}
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
	[Rpc.Owner]
	public void TeleportRpc(Vector3 pos, Rotation rot)
	{
		if (_controller != null)
			_controller.Enabled = false;
		WorldPosition = pos + Vector3.Up * 5f;
		WorldRotation = rot;
		if (_controller != null) _controller.Enabled = true;
	}

	public void ResetForLobby()
	{
		if (!Networking.IsHost) return;

		Role = PlayerRole.None;
		AssignedRole = PlayerRole.None;
		IsAlive = true;

		CleanupRagdollRpc();

		// Réactive les composants désactivés par HandleDeathState
		if (_controller != null) _controller.Enabled = true;

		var interactor = GameObject.GetComponent<PlayerInteractor>();
		if (interactor != null) interactor.Enabled = true;

		var inventory = GameObject.GetComponent<PlayerInventory>();
		if (inventory != null)
		{
			inventory.Enabled = true;
			// Vide les 3 slots
			inventory.Slot0 = ItemType.None;
			inventory.Slot1 = ItemType.None;
			inventory.Slot2 = ItemType.None;
			inventory.ActiveSlot = 0;
		}

		var flashlight = GameObject.GetComponentInChildren<Flashlight>();
		if (flashlight != null) flashlight.Enabled = true;
	}
}