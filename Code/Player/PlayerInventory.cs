using System.Collections.Generic;

/// <summary>
/// Gère l'inventaire du joueur : 3 slots, slot actif, switch via touches ou molette.
/// </summary>
public sealed class PlayerInventory : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }

	public const int SlotCount = 3;

	[Sync(SyncFlags.FromHost)] public ItemType Slot0 { get; set; } = ItemType.None;
	[Sync(SyncFlags.FromHost)] public ItemType Slot1 { get; set; } = ItemType.None;
	[Sync(SyncFlags.FromHost)] public ItemType Slot2 { get; set; } = ItemType.None;

	[Sync(SyncFlags.FromHost)] public int ActiveSlot { get; set; } = 0;

	protected override void OnUpdate()
	{
		if (TargetPlayer == null || TargetPlayer.IsProxy)
			return;

		if (!TargetPlayer.IsAlive)
			return;

		HandleSlotSwitching();
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

	[Rpc.Broadcast]
	private void SetActiveSlotRpc(int slotIndex)
	{
		if (!Networking.IsHost) return;
		if (slotIndex < 0 || slotIndex >= SlotCount) return;

		ActiveSlot = slotIndex;
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
}