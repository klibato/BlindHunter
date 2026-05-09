using System.Linq;

/// <summary>
/// Composant pour un panneau qui ne s'active que si le joueur possède une Keycard équipée.
/// Consomme la Keycard de son inventaire à l'activation.
/// </summary>
public sealed class KeycardReader : Component
{
	[Property] public string PromptWithKeycard { get; set; } = "Use Keycard";
	[Property] public string PromptWithoutKeycard { get; set; } = "Need Keycard equipped";
	[Property] public SoundEvent InsertSound { get; set; }

	private Interactable _interactable;

	protected override void OnStart()
	{
		_interactable = GetComponent<Interactable>();
		if (_interactable == null)
		{
			Log.Warning($"KeycardReader on {GameObject.Name} requires an Interactable component!");
			return;
		}

		// Validation : seul un joueur avec Keycard équipée peut activer
		_interactable.CanInteract = (interactor) =>
		{
			var inventory = interactor.GameObject.GetComponent<PlayerInventory>();
			if (inventory == null) return false;
			return inventory.GetActiveItem() == ItemType.Keycard;
		};

		_interactable.OnInteracted += OnReaderUsed;
	}

	protected override void OnUpdate()
	{
		// Met à jour dynamiquement le prompt selon ce que le joueur local équipe
		if (_interactable == null) return;
		if (_interactable.IsCompleted) return;

		var localPlayer = Scene.GetAllComponents<PlayerSetup>()
			.FirstOrDefault(p => !p.IsProxy);

		if (localPlayer == null) return;

		var inventory = localPlayer.GameObject.GetComponent<PlayerInventory>();
		if (inventory == null) return;

		bool hasKeycard = inventory.GetActiveItem() == ItemType.Keycard;
		_interactable.PromptText = hasKeycard ? PromptWithKeycard : PromptWithoutKeycard;
	}

	private void OnReaderUsed(PlayerSetup interactor)
	{
		if (!Networking.IsHost) return;

		var inventory = interactor.GameObject.GetComponent<PlayerInventory>();
		if (inventory == null) return;

		// Consomme la Keycard du slot actif
		inventory.RemoveActiveItem();

		PlayInsertSoundRpc(WorldPosition);

		Log.Info($"Keycard consumed by {interactor.GameObject.Name}");
	}

	[Rpc.Broadcast]
	private void PlayInsertSoundRpc(Vector3 position)
	{
		if (InsertSound != null) Sound.Play(InsertSound, position);
	}
}