using System;
using System.Linq;

public sealed class KillerAttack : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }
	[Property] public float AttackRange { get; set; } = 100f;

	[Property] public float MaxStamina { get; set; } = 3f;
	[Property] public float StaminaCost { get; set; } = 1f;
	[Property] public float StaminaRegen { get; set; } = 0.4f;
	[Property] public float MinStaminaToAttack { get; set; } = 1f;

	[Property] public SoundEvent HitSound { get; set; }   // joue quand le killer touche un survivor (impact)
	[Property] public SoundEvent MissSound { get; set; }  // joue quand l'attaque rate (whoosh)

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

		if ( CurrentStamina < MaxStamina )
		{
			CurrentStamina += StaminaRegen * Time.Delta;
			CurrentStamina = MathF.Min( CurrentStamina, MaxStamina );
		}

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

		PlayAttackSoundRpc( WorldPosition, hitSomeone );
	}

	[Rpc.Broadcast]
	private void PlayAttackSoundRpc( Vector3 position, bool wasHit )
	{
		// Audio entendu par tous : hit = impact charnu, miss = whoosh
		var sound = wasHit ? HitSound : MissSound;
		if ( sound != null )
		{
			Sound.Play( sound, position );
		}
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