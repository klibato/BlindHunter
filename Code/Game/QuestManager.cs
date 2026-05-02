/// <summary>
/// Tracks quest completion. Subscribes to each <see cref="Interactable.OnInteracted"/> event on the
/// host instead of scanning the scene every frame.
/// </summary>
public sealed class QuestManager : Component
{
	[Sync( SyncFlags.FromHost )] public int CompletedQuests { get; set; }

	public int TotalQuests { get; private set; }

	public static QuestManager Instance { get; private set; }

	public bool AllQuestsCompleted => CompletedQuests >= TotalQuests && TotalQuests > 0;

	protected override void OnAwake()
	{
		GameObject.NetworkMode = NetworkMode.Object;
		Instance = this;
	}

	protected override void OnStart()
	{
		var questObjects = Scene.GetAllComponents<Interactable>()
			.Where( q => q.IsQuestObject )
			.ToList();

		TotalQuests = questObjects.Count;

		// Only the host maintains CompletedQuests; subscribe to events here so the
		// count is incremented exactly once per completion rather than polled every tick.
		if ( Networking.IsHost )
		{
			foreach ( var interactable in questObjects )
				interactable.OnInteracted += OnQuestCompleted;
		}
	}

	private void OnQuestCompleted( PlayerSetup _ )
	{
		CompletedQuests++;
	}
}
