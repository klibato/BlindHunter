using System.Collections.Generic;
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

	private List<QuestGroup> _questGroups = new();
	public IReadOnlyList<QuestGroup> QuestGroups => _questGroups;

	protected override void OnAwake()
	{
		Instance = this;
	}

	protected override void OnStart()
	{
		_questGroups = Scene.GetAllComponents<QuestGroup>().ToList();

		if ( !Networking.IsHost ) return;

		var standaloneInteractables = Scene.GetAllComponents<Interactable>()
			.Where( i => i.IsQuestObject )
			.ToList();

		TotalQuests = standaloneInteractables.Count + _questGroups.Count;

		foreach ( var i in standaloneInteractables )
		{
			i.OnInteracted += OnQuestCompleted;
		}
		foreach ( var g in _questGroups )
		{
			g.OnGroupCompleted += OnGroupCompleted;
		}

		Log.Info( $"QuestManager initialized: {TotalQuests} total quests ({standaloneInteractables.Count} standalone + {_questGroups.Count} groups)" );
	}

	private void OnQuestCompleted( PlayerSetup interactor )
	{
		if ( !Networking.IsHost ) return;
		CompletedQuests++;
	}

	private void OnGroupCompleted()
	{
		if ( !Networking.IsHost ) return;
		CompletedQuests++;
	}
}