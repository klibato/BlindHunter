using Sandbox;

public sealed class ExitDoor : Component
{
	private Interactable _interactable;

	protected override void OnStart()
	{
		_interactable = GetComponent<Interactable>();
		if ( _interactable == null )
		{
			Log.Warning( $"ExitDoor on {GameObject.Name} requires an Interactable component!" );
			return;
		}

		// On écoute l'événement d'interaction
		_interactable.OnInteracted += OnDoorInteracted;
	}

	protected override void OnUpdate()
	{
		// Met à jour le PromptText selon l'état des quêtes
		if ( _interactable == null ) return;
		if ( QuestManager.Instance == null ) return;

		bool unlocked = QuestManager.Instance.AllQuestsCompleted;

		_interactable.PromptText = unlocked 
			? "Escape through the door" 
			: $"Door locked ({QuestManager.Instance.CompletedQuests}/{QuestManager.Instance.TotalQuests})";
	}

	private void OnDoorInteracted( PlayerSetup interactor )
	{
		// Cette méthode tourne chez le host (parce que Interactable.Interact filtre IsHost)
		if ( !Networking.IsHost ) return;

		// Vérifie que les quêtes sont toutes faites
		if ( QuestManager.Instance == null ) return;

		if ( !QuestManager.Instance.AllQuestsCompleted )
		{
			Log.Info( "Door interacted but not all quests are done. Resetting completion." );
			// On annule la complétion de l'Interactable pour que le joueur puisse réessayer plus tard
			_interactable.IsCompleted = false;
			return;
		}

		// Toutes les quêtes sont faites → victoire
		if ( GameStateManager.Instance != null )
		{
			GameStateManager.Instance.DeclareSurvivorsVictory();
		}
	}
}