using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Groupe de sous-quêtes. La quête principale est complétée quand toutes les sous-quêtes le sont.
/// Les sous-quêtes sont les Interactable enfants de ce GameObject (ou listés manuellement).
/// </summary>
public sealed class QuestGroup : Component
{
	[Property] public string GroupName { get; set; } = "Generators";
	[Property] public List<Interactable> SubQuests { get; set; } = new();

	[Sync(SyncFlags.FromHost)] public bool IsCompleted { get; set; }

	public event Action OnGroupCompleted;

	protected override void OnStart()
	{
		// Auto-detect sub-quests si la liste est vide : prend tous les Interactable enfants
		if ( SubQuests.Count == 0 )
		{
			SubQuests = GetComponentsInChildren<Interactable>().ToList();
		}

		// Force IsQuestObject = false sur les sous-quêtes
		// (pour qu'elles ne comptent PAS individuellement dans QuestManager)
		foreach ( var sub in SubQuests )
		{
			if ( sub != null )
			{
				sub.IsQuestObject = false;
				sub.OnInteracted += OnSubQuestCompleted;
			}
		}
	}

	private void OnSubQuestCompleted( PlayerSetup interactor )
	{
		if ( !Networking.IsHost ) return;

		// Vérifie si toutes les sous-quêtes sont done
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