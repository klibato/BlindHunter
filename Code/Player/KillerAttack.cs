using System;
using System.Linq;

/// <summary>
/// Component qui gère l'attaque du tueur en mêlée.
/// </summary>
public sealed class KillerAttack : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }
	[Property] public float AttackRange { get; set; } = 100f;
	[Property] public float AttackCooldown { get; set; } = 1f;
	[Property] public float MissNoiseIntensity { get; set; } = 200f;
	[Property] public float HitNoiseIntensity { get; set; } = 350f;

	private CameraComponent _camera;
	private float _nextAttackTime;

	protected override void OnStart()
	{
		_camera = GetComponentInChildren<CameraComponent>();
	}

	protected override void OnUpdate()
	{
		if ( TargetPlayer == null || TargetPlayer.IsProxy )
			return;
		if ( TargetPlayer.Role != PlayerRole.Killer )
			return;
		if ( !TargetPlayer.IsAlive )
			return;
		if ( !Input.Pressed( "Attack1" ) )
			return;
		if ( RealTime.Now < _nextAttackTime )
			return;

		PerformAttack();
		_nextAttackTime = RealTime.Now + AttackCooldown;
	}

	private void PerformAttack()
	{
		if ( _camera == null )
			return;

		var ray = new Ray( _camera.WorldPosition, _camera.WorldRotation.Forward );
		var trace = Scene.Trace
			.Ray( ray, AttackRange )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		// Détermine le bruit selon que l'attaque touche ou pas
		bool hitSomeone = false;

		if ( trace.Hit && trace.GameObject != null )
		{
			var hitPlayer = trace.GameObject.GetComponentInParent<PlayerSetup>();
			if ( hitPlayer != null && hitPlayer.Role == PlayerRole.Survivor && hitPlayer.IsAlive )
			{
				hitSomeone = true;
				KillRpc( hitPlayer.GameObject.Id );
			}
		}

		// Émet un bruit à la position du tueur
		float noiseIntensity = hitSomeone ? HitNoiseIntensity : MissNoiseIntensity;
		EmitAttackNoiseRpc( WorldPosition, noiseIntensity );
	}

	[Rpc.Broadcast]
	private void EmitAttackNoiseRpc( Vector3 position, float intensity )
	{
		var localPlayer = Scene.GetAllComponents<PlayerSetup>()
			.FirstOrDefault( p => !p.IsProxy );
		if ( localPlayer == null ) return;
		if ( localPlayer.Role != PlayerRole.Killer ) return;

		NoiseVisualizer.AddNoise( position, intensity );
	}

	[Rpc.Broadcast]
	private void KillRpc( Guid playerId )
	{
		if ( !Networking.IsHost ) return;

		var go = Scene.Directory.FindByGuid( playerId );
		if ( go == null ) return;

		var setup = go.GetComponent<PlayerSetup>();
		if ( setup == null ) return;

		setup.Kill();
	}
}