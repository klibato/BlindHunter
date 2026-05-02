using Sandbox;
using System.Threading.Tasks;
public sealed class GameManager : Component, Component.INetworkListener
{
	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public Vector3 SpawnPosition { get; set; } = new Vector3(0, 0, 50);

	protected override async Task OnLoad()
	{
		if (Scene.IsEditor)
			return;

		if (!Networking.IsActive)
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds(0.1f);
			Networking.CreateLobby();
		}
	}

	void INetworkListener.OnActive(Connection channel)
	{
		if (!Networking.IsHost)
			return;

		if (PlayerPrefab is null)
		{
			Log.Warning("PlayerPrefab is not set on GameManager!");
			return;
		}

		var playerObject = PlayerPrefab.Clone(SpawnPosition);
		playerObject.Name = $"Player - {channel.DisplayName}";
		playerObject.NetworkSpawn(channel);
	}
}