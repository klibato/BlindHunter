using Sandbox;
using System.Linq;

public sealed class QuestManager : Component
{
	[Sync(SyncFlags.FromHost)] public int CompletedQuests { get; set; }

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
		RecountQuests();
	}

	protected override void OnUpdate()
	{
		if ( Networking.IsHost )
		{
			RecountQuests();
		}
	}

	private void RecountQuests()
	{
		var allQuests = Scene.GetAllComponents<Interactable>().ToList();
		TotalQuests = allQuests.Count;
		CompletedQuests = allQuests.Count( q => q.IsCompleted );
	}
}