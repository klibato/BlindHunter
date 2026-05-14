using System.Linq;

/// <summary>
/// Composant pour un panneau qui ne s'active que si le joueur possède une Keycard équipée.
/// Consomme la Keycard de son inventaire à l'activation.
/// </summary>
public sealed class KeycardReader : Component
{
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

		// Prompt dynamique selon que le joueur local a une keycard équipée
		_interactable.LocalizedPromptProvider = () =>
		{
			var localPlayer = Scene.GetAllComponents<PlayerSetup>()
				.FirstOrDefault(p => !p.IsProxy);
			if (localPlayer == null) return Lang.Get("prompt.keycard.without");

			var inventory = localPlayer.GameObject.GetComponent<PlayerInventory>();
			if (inventory == null) return Lang.Get("prompt.keycard.without");

			bool hasKeycard = inventory.GetActiveItem() == ItemType.Keycard;
			return Lang.Get(hasKeycard ? "prompt.keycard.with" : "prompt.keycard.without");
		};

		_interactable.OnInteracted += OnReaderUsed;
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
