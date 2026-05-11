/// <summary>
/// Composant attaché à un item ramassable. Ajoute un ItemType à l'inventaire du joueur qui interagit.
/// </summary>
public sealed class Pickable : Component
{
	public static readonly List<Pickable> All = new();

	[Property] public ItemType ItemKind { get; set; } = ItemType.Stone;
	[Property] public SoundEvent PickupSound { get; set; }

	private Interactable _interactable;

	protected override void OnStart()
	{
		All.Add(this);

		_interactable = GetComponent<Interactable>();
		if (_interactable == null)
		{
			Log.Warning($"Pickable on {GameObject.Name} requires an Interactable component!");
			return;
		}

		_interactable.IsQuestObject = false;
		_interactable.PromptText = $"Pick up {ItemKind}";
		_interactable.OnInteracted += OnPickedUp;
	}

	protected override void OnDestroy()
	{
		All.Remove(this);
	}

	private void OnPickedUp(PlayerSetup interactor)
	{
		if (!Networking.IsHost) return;

		var inventory = interactor.GameObject.GetComponent<PlayerInventory>();
		if (inventory == null) return;

		bool added = inventory.TryAddItem(ItemKind);
		if (!added)
		{
			_interactable.IsCompleted = false;
			Log.Info($"Inventory full, can't pick up {ItemKind}");
			return;
		}

		PlayPickupSoundRpc(WorldPosition);

		// Cache l'objet au lieu de le détruire pour pouvoir le réactiver au reset
		GameObject.Enabled = false;
	}

	public void ResetPickable()
	{
		if (!Networking.IsHost) return;
		GameObject.Enabled = true;
		if (_interactable != null) _interactable.IsCompleted = false;
	}

	[Rpc.Broadcast]
	private void PlayPickupSoundRpc(Vector3 position)
	{
		if (PickupSound != null) Sound.Play(PickupSound, position);
	}
}