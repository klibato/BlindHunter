/// <summary>
/// Component temporaire ajouté à un objet quand il est lancé.
/// Écoute les collisions et émet un noise event au premier impact, puis se retire.
/// </summary>
using System.Linq;
using System.Threading.Tasks;
public sealed class ThrowableTracker : Component, Component.ICollisionListener
{
	[Property] public float NoiseIntensity { get; set; } = 300f;
	[Property] public float Lifetime { get; set; } = 5f;

	private float _spawnTime;
	private bool _impacted;

	protected override void OnStart()
	{
		_spawnTime = RealTime.Now;
	}

	protected override void OnUpdate()
	{
		if (!Networking.IsHost) return;
		if (_impacted) return;

		// Auto-cleanup après Lifetime même sans impact (objet qui flotte sans rien toucher)
		if (RealTime.Now - _spawnTime > Lifetime)
		{
			Destroy();
		}
	}

	public void OnCollisionStart(Collision other)
	{
		if (!Networking.IsHost) return;
		if (_impacted) return;

		var hitPlayer = other.Other.GameObject?.GetComponentInParent<PlayerSetup>();
		if (hitPlayer != null) return;

		_impacted = true;
		EmitNoiseRpc(WorldPosition, NoiseIntensity);

		// Détruit le cube entier (pas juste le tracker) après un court délai
		DestroyGameObjectDelayed(); // 2 secondes après l'impact
	}
	private async void DestroyGameObjectDelayed()
	{
		await Task.Delay(2000);
		if (GameObject.IsValid())
		{
			GameObject.Destroy();
		}
	}
	public void OnCollisionUpdate(Collision other) { }
	public void OnCollisionStop(CollisionStop other) { }

	[Rpc.Broadcast]
	private void EmitNoiseRpc(Vector3 position, float intensity)
	{
		var localPlayer = Scene.GetAllComponents<PlayerSetup>()
			.FirstOrDefault(p => !p.IsProxy);

		if (localPlayer == null) return;
		if (localPlayer.Role != PlayerRole.Killer) return;

		NoiseVisualizer.AddNoise(position, intensity);
	}
}