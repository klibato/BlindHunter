using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Groupe de sous-quêtes. La quête principale est complétée quand toutes les sous-quêtes le sont.
/// </summary>
public sealed class QuestGroup : Component
{
	[Property] public string GroupName { get; set; } = "Generators";
	[Property] public List<Interactable> SubQuests { get; set; } = new();

	[Sync(SyncFlags.FromHost)] public bool IsCompleted { get; set; }

	public event Action OnGroupCompleted;

	protected override void OnAwake()
	{
		// Auto-detect si vide
		if ( SubQuests.Count == 0 )
		{
			SubQuests = GetComponentsInChildren<Interactable>().ToList();
		}

		// IMPORTANT : marquer les sous-quêtes comme non-quêtes AVANT que QuestManager les compte
		foreach ( var sub in SubQuests )
		{
			if ( sub != null )
			{
				sub.IsQuestObject = false;
			}
		}
	}

	protected override void OnStart()
	{
		// Subscribe aux events des sous-quêtes (host only pour éviter doublon)
		if ( !Networking.IsHost ) return;

		foreach ( var sub in SubQuests )
		{
			if ( sub != null )
			{
				sub.OnInteracted += OnSubQuestCompleted;
			}
		}
	}

	private void OnSubQuestCompleted( PlayerSetup interactor )
	{
		if ( !Networking.IsHost ) return;

		bool allDone = SubQuests.All( s => s != null && s.IsCompleted );

		if ( allDone && !IsCompleted )
		{
			IsCompleted = true;
			Log.Info( $"QuestGroup '{GroupName}' completed!" );
			OnGroupCompleted?.Invoke();
		}
	}

	public int CompletedCount => SubQuests.Count( s => s != null && s.IsCompleted );
	public int TotalCount => SubQuests.Count;
}