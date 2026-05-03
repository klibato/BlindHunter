using System.Collections.Generic;

/// <summary>
/// Gère l'inventaire du joueur : 3 slots, slot actif, switch via touches ou molette.
/// </summary>
public sealed class PlayerInventory : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }
	[Property] public GameObject StonePrefab { get; set; }
	[Property] public float ThrowForce { get; set; } = 1500f;

	public const int SlotCount = 3;

	[Sync(SyncFlags.FromHost)] public ItemType Slot0 { get; set; } = ItemType.None;
	[Sync(SyncFlags.FromHost)] public ItemType Slot1 { get; set; } = ItemType.None;
	[Sync(SyncFlags.FromHost)] public ItemType Slot2 { get; set; } = ItemType.None;

	[Sync(SyncFlags.FromHost)] public int ActiveSlot { get; set; } = 0;

	private CameraComponent _camera;

	protected override void OnStart()
	{
		_camera = GetComponentInChildren<CameraComponent>();
	}

	protected override void OnUpdate()
	{
		if (TargetPlayer == null || TargetPlayer.IsProxy)
			return;

		if (!TargetPlayer.IsAlive)
			return;

		HandleSlotSwitching();
		HandleItemUsage();
	}

	private void HandleSlotSwitching()
	{
		// Touches 1, 2, 3
		if (Input.Pressed("Slot1"))
			SetActiveSlotRpc(0);
		else if (Input.Pressed("Slot2"))
			SetActiveSlotRpc(1);
		else if (Input.Pressed("Slot3"))
			SetActiveSlotRpc(2);

		// Molette souris
		float wheel = Input.MouseWheel.y;
		if (wheel > 0.1f)
		{
			int next = (ActiveSlot - 1 + SlotCount) % SlotCount;
			SetActiveSlotRpc(next);
		}
		else if (wheel < -0.1f)
		{
			int next = (ActiveSlot + 1) % SlotCount;
			SetActiveSlotRpc(next);
		}
	}

	private void HandleItemUsage()
	{
		if (!Input.Pressed("Attack1")) return;
		if (TargetPlayer.Role != PlayerRole.Survivor) return;

		var item = GetActiveItem();
		if (item == ItemType.None) return;

		if (item == ItemType.Stone)
		{
			if (_camera == null) return;
			ThrowStoneRpc(_camera.WorldPosition, _camera.WorldRotation.Forward);
		}
		// Keycard : sera géré à l'étape 3
	}

	[Rpc.Broadcast]
	private void SetActiveSlotRpc(int slotIndex)
	{
		if (!Networking.IsHost) return;
		if (slotIndex < 0 || slotIndex >= SlotCount) return;

		ActiveSlot = slotIndex;
	}

	[Rpc.Broadcast]
	private void ThrowStoneRpc(Vector3 spawnPos, Vector3 direction)
	{
		if (!Networking.IsHost) return;
		if (StonePrefab == null) return;

		// Spawn la pierre devant la caméra
		var stone = StonePrefab.Clone(spawnPos + direction * 50f);
		stone.NetworkSpawn();

		var rb = stone.GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.Velocity = direction * ThrowForce;
		}

		// Attache un tracker temporaire pour détecter l'impact
		stone.Components.Create<ThrowableTracker>();

		// Vide le slot actif
		RemoveActiveItem();
	}

	/// <summary>
	/// Récupère l'ItemType d'un slot par index.
	/// </summary>
	public ItemType GetSlot(int index)
	{
		return index switch
		{
			0 => Slot0,
			1 => Slot1,
			2 => Slot2,
			_ => ItemType.None
		};
	}

	/// <summary>
	/// Retourne l'ItemType actuellement actif (ce que le joueur tient).
	/// </summary>
	public ItemType GetActiveItem()
	{
		return GetSlot(ActiveSlot);
	}

	/// <summary>
	/// Ajoute un item dans le premier slot vide. Retourne false si l'inventaire est plein.
	/// </summary>
	public bool TryAddItem(ItemType item)
	{
		if (!Networking.IsHost) return false;
		if (item == ItemType.None) return false;

		if (Slot0 == ItemType.None) { Slot0 = item; return true; }
		if (Slot1 == ItemType.None) { Slot1 = item; return true; }
		if (Slot2 == ItemType.None) { Slot2 = item; return true; }

		return false;
	}

	/// <summary>
	/// Vide le slot actif (utilisé après avoir lancé/utilisé l'item).
	/// </summary>
	public void RemoveActiveItem()
	{
		if (!Networking.IsHost) return;

		switch (ActiveSlot)
		{
			case 0: Slot0 = ItemType.None; break;
			case 1: Slot1 = ItemType.None; break;
			case 2: Slot2 = ItemType.None; break;
		}
	}
}