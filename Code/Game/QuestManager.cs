using System.Linq;

/// <summary>
/// Tracks completion of all quests (Interactable + QuestGroup) in the scene.
/// </summary>
public sealed class QuestManager : Component
{
	[Sync(SyncFlags.FromHost)] public int CompletedQuests { get; set; }
	[Sync(SyncFlags.FromHost)] public int TotalQuests { get; set; }

	public bool AllQuestsCompleted => TotalQuests > 0 && CompletedQuests >= TotalQuests;

	public static QuestManager Instance { get; private set; }

	protected override void OnAwake()
	{
		Instance = this;
	}

	protected override void OnStart()
	{
		if ( !Networking.IsHost ) return;

		// Compte les Interactable standalone (avec IsQuestObject = true)
		var standaloneInteractables = Scene.GetAllComponents<Interactable>()
			.Where( i => i.IsQuestObject )
			.ToList();

		// Compte les QuestGroup (chaque groupe = 1 quête)
		var questGroups = Scene.GetAllComponents<QuestGroup>().ToList();

		TotalQuests = standaloneInteractables.Count + questGroups.Count;

		// Subscribe aux events
		foreach ( var i in standaloneInteractables )
		{
			i.OnInteracted += OnQuestCompleted;
		}
		foreach ( var g in questGroups )
		{
			g.OnGroupCompleted += OnGroupCompleted;
		}

		Log.Info( $"QuestManager initialized: {TotalQuests} total quests ({standaloneInteractables.Count} standalone + {questGroups.Count} groups)" );
	}

	private void OnQuestCompleted( PlayerSetup interactor )
	{
		if ( !Networking.IsHost ) return;
		CompletedQuests++;
		Log.Info( $"Quest completed: {CompletedQuests}/{TotalQuests}" );
	}

	private void OnGroupCompleted()
	{
		if ( !Networking.IsHost ) return;
		CompletedQuests++;
		Log.Info( $"Quest group completed: {CompletedQuests}/{TotalQuests}" );
	}
}