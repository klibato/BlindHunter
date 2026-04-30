using Sandbox;
using System.Linq;

public sealed class PlayerSetup : Component
{
	[Sync( SyncFlags.FromHost )] public PlayerRole Role { get; set; } = PlayerRole.None;

	private float _noiseTimer;
	private const float NOISE_INTERVAL = 0.5f; // émettre du bruit max toutes les 0.5s
	private const float MOVEMENT_THRESHOLD = 50f; // vitesse min pour émettre du bruit

	protected override void OnStart()
	{
		Log.Info( $"PlayerSetup.OnStart - IsProxy: {IsProxy}, Role: {Role}, GameObject: {GameObject.Name}" );

		if ( Networking.IsHost )
		{
			int killersCount = Scene.GetAllComponents<PlayerSetup>().Count( p => p.Role == PlayerRole.Killer );
			Role = killersCount == 0 ? PlayerRole.Killer : PlayerRole.Survivor;
			Log.Info( $"Host assigned role: {Role} (killers count was {killersCount})" );
		}

		if ( IsProxy )
		{
			var camera = GetComponentInChildren<CameraComponent>();
			if ( camera != null )
				camera.Enabled = false;
		}
	}

	protected override void OnUpdate()
	{
		ApplyRoleColor();
		HandleNoiseEmission();
	}

	private void HandleNoiseEmission()
	{
		// On n'émet du bruit QUE pour le joueur local (pas les proxies)
		if ( IsProxy ) return;

		// Récupère le PlayerController pour avoir la vitesse
		var controller = GetComponent<PlayerController>();
		if ( controller == null ) return;

		// Si le joueur bouge assez vite et que le timer permet
		float speed = controller.Velocity.Length;
		if ( speed > MOVEMENT_THRESHOLD && _noiseTimer <= 0f )
		{
			EmitNoise( WorldPosition, speed );
			_noiseTimer = NOISE_INTERVAL;
		}

		_noiseTimer -= Time.Delta;
	}

	[Rpc.Broadcast]
	private void EmitNoise( Vector3 position, float intensity )
	{
		// Reçu par tous les clients
		// Mais seul le tueur visualise

		var localPlayer = Scene.GetAllComponents<PlayerSetup>().FirstOrDefault( p => !p.IsProxy );
		if ( localPlayer == null ) return;

		if ( localPlayer.Role != PlayerRole.Killer ) return;

		// Visualise sous forme de gizmo (cercle au sol)
		// On stocke ça pour l'afficher pendant quelques secondes
		NoiseVisualizer.AddNoise( position, intensity );
	}

	private void ApplyRoleColor()
	{
		var renderer = GetComponentInChildren<SkinnedModelRenderer>();
		if ( renderer == null )
			return;

		Color targetColor = Role switch
		{
			PlayerRole.Killer => Color.Red,
			PlayerRole.Survivor => Color.Blue,
			_ => Color.White
		};

		renderer.Tint = targetColor;
	}
}