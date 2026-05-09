/// <summary>Requires all quests to be completed before triggering survivor victory.</summary>
public sealed class ExitDoor : Component
{
	[Property] public SoundEvent OpenSound { get; set; }

	private Interactable _interactable;

	protected override void OnStart()
	{
		_interactable = GetComponent<Interactable>();
		if ( _interactable == null )
		{
			Log.Warning( $"ExitDoor on {GameObject.Name} requires an Interactable component!" );
			return;
		}

		_interactable.OnInteracted += OnDoorInteracted;
	}

	protected override void OnUpdate()
	{
		if ( _interactable == null ) return;
		if ( QuestManager.Instance == null ) return;

		bool unlocked = QuestManager.Instance.AllQuestsCompleted;
		_interactable.PromptText = unlocked
			? "Escape through the door"
			: $"Door locked ({QuestManager.Instance.CompletedQuests}/{QuestManager.Instance.TotalQuests})";
	}

	private void OnDoorInteracted( PlayerSetup interactor )
	{
		if ( !Networking.IsHost ) return;
		if ( QuestManager.Instance == null ) return;

		if ( !QuestManager.Instance.AllQuestsCompleted )
		{
			// Reset so the player can retry once all quests are done.
			_interactable.IsCompleted = false;
			return;
		}

		PlayOpenSoundRpc( WorldPosition );
		GameStateManager.Instance?.DeclareSurvivorsVictory();
	}

	[Rpc.Broadcast]
	private void PlayOpenSoundRpc( Vector3 position )
	{
		if ( OpenSound != null ) Sound.Play( OpenSound, position );
	}
}
