using System;
using System.Linq;

public sealed class KillerAttack : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }
	[Property] public float AttackRange { get; set; } = 100f;
	[Property] public float MissNoiseIntensity { get; set; } = 200f;
	[Property] public float HitNoiseIntensity { get; set; } = 350f;

	[Property] public float MaxStamina { get; set; } = 3f; // 3 attaques avant épuisement
	[Property] public float StaminaCost { get; set; } = 1f; // par attaque
	[Property] public float StaminaRegen { get; set; } = 0.4f; // par seconde
	[Property] public float MinStaminaToAttack { get; set; } = 1f;

	[Sync] public float CurrentStamina { get; set; }

	private CameraComponent _camera;

	protected override void OnStart()
	{
		_camera = GetComponentInChildren<CameraComponent>();
		CurrentStamina = MaxStamina;
	}

	protected override void OnUpdate()
	{
		if ( TargetPlayer == null || TargetPlayer.IsProxy ) return;
		if ( TargetPlayer.Role != PlayerRole.Killer ) return;
		if ( !TargetPlayer.IsAlive ) return;

		// Regen stamina
		if ( CurrentStamina < MaxStamina )
		{
			CurrentStamina += StaminaRegen * Time.Delta;
			CurrentStamina = MathF.Min( CurrentStamina, MaxStamina );
		}

		// Attaque
		if ( !Input.Pressed( "Attack1" ) ) return;
		if ( CurrentStamina < MinStaminaToAttack ) return;

		PerformAttack();
		CurrentStamina -= StaminaCost;
	}

	private void PerformAttack()
	{
		if ( _camera == null ) return;

		var ray = new Ray( _camera.WorldPosition, _camera.WorldRotation.Forward );
		var trace = Scene.Trace
			.Ray( ray, AttackRange )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

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

	public float StaminaPercent => MaxStamina > 0f ? CurrentStamina / MaxStamina : 0f;
}