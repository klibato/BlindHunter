using System;

/// <summary>
/// Gère la capacité du survivant à ramasser, porter et lancer un objet physique.
/// </summary>
public sealed class PlayerCarrier : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }
	[Property] public PlayerInteractor TargetInteractor { get; set; }
	[Property] public float CarryDistance { get; set; } = 80f;
	[Property] public float ThrowForce { get; set; } = 1500f;

	[Sync(SyncFlags.FromHost)] public GameObject CarriedObject { get; set; }

	private CameraComponent _camera;

	protected override void OnStart()
	{
		_camera = GetComponentInChildren<CameraComponent>();
	}

	protected override void OnUpdate()
	{
		if (TargetPlayer == null || TargetPlayer.IsProxy)
			return;

		if (TargetPlayer.Role != PlayerRole.Survivor)
			return;

		if (!TargetPlayer.IsAlive)
			return;

		UpdateCarriedObjectPosition();

		if (CarriedObject == null && Input.Pressed("Use"))
		{
			TryPickup();
		}
		else if (CarriedObject != null && Input.Pressed("Attack1"))
		{
			ThrowCarriedObject();
		}
	}

	private void UpdateCarriedObjectPosition()
	{
		if (CarriedObject == null || _camera == null) return;
		if (!CarriedObject.IsValid()) return;

		var targetPos = _camera.WorldPosition + _camera.WorldRotation.Forward * CarryDistance;
		CarriedObject.WorldPosition = targetPos;
	}

	private void TryPickup()
	{
		if (TargetInteractor == null) return;
		if (_camera == null) return;

		var ray = new Ray(_camera.WorldPosition, _camera.WorldRotation.Forward);
		var trace = Scene.Trace
			.Ray(ray, TargetInteractor.InteractionRange)
			.IgnoreGameObjectHierarchy(GameObject)
			.Run();

		if (!trace.Hit || trace.GameObject == null) return;

		var rb = trace.GameObject.GetComponent<Rigidbody>();
		if (rb == null) return;

		var playerSetup = trace.GameObject.GetComponentInParent<PlayerSetup>();
		if (playerSetup != null) return;

		PickupRpc(trace.GameObject.Id);
	}

	[Rpc.Broadcast]
	private void PickupRpc(Guid objectId)
	{
		if (!Networking.IsHost) return;

		var go = Scene.Directory.FindByGuid(objectId);
		if (go == null) return;

		var rb = go.GetComponent<Rigidbody>();
		if (rb == null) return;

		rb.MotionEnabled = false;
		go.Network.AssignOwnership(Network.Owner);

		CarriedObject = go;
	}

	private void ThrowCarriedObject()
	{
		if (CarriedObject == null) return;
		if (_camera == null) return;

		ThrowRpc(CarriedObject.Id, _camera.WorldRotation.Forward);
	}

	[Rpc.Broadcast]
	private void ThrowRpc(Guid objectId, Vector3 direction)
	{
		if (!Networking.IsHost) return;

		var go = Scene.Directory.FindByGuid(objectId);
		if (go == null) return;

		var rb = go.GetComponent<Rigidbody>();
		if (rb == null) return;

		rb.MotionEnabled = true;
		rb.Velocity = direction * ThrowForce;
		go.Network.DropOwnership();

		// Attache un tracker temporaire qui détectera l'impact via collision
		var tracker = go.Components.Create<ThrowableTracker>();

		CarriedObject = null;
	}
}