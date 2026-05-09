using System.Linq;
using System;
/// <summary>
/// Active echolocation for the killer : pure wave emission with a cooldown.
/// Detached from KillerAttack so the killer can scan without swinging
/// (avoids spamming the miss whoosh just to "see").
/// </summary>
public sealed class KillerEchoActive : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }
	[Property] public float EchoNoiseIntensity { get; set; } = 250f;
	[Property] public float EchoCooldown { get; set; } = 1.5f;
	[Property] public SoundEvent EchoSound { get; set; }

	[Sync] public float CurrentCooldown { get; set; }

	protected override void OnUpdate()
	{
		if ( TargetPlayer == null || TargetPlayer.IsProxy ) return;
		if ( TargetPlayer.Role != PlayerRole.Killer ) return;
		if ( !TargetPlayer.IsAlive ) return;

		if ( CurrentCooldown > 0f )
		{
			CurrentCooldown -= Time.Delta;
		}

		if ( !Input.Pressed( "Attack2" ) ) return;
		if ( CurrentCooldown > 0f ) return;

		EmitEchoNoiseRpc( WorldPosition, EchoNoiseIntensity );
		PlayEchoSoundRpc( WorldPosition );
		CurrentCooldown = EchoCooldown;
	}

	[Rpc.Broadcast]
	private void EmitEchoNoiseRpc( Vector3 position, float intensity )
	{
		// Visible côté killer local uniquement (drive le shader)
		var localPlayer = Scene.GetAllComponents<PlayerSetup>()
			.FirstOrDefault( p => !p.IsProxy );
		if ( localPlayer == null || localPlayer.Role != PlayerRole.Killer ) return;

		NoiseVisualizer.AddNoise( position, intensity );
	}

	[Rpc.Broadcast]
	private void PlayEchoSoundRpc( Vector3 position )
	{
		if ( EchoSound != null )
		{
			Sound.Play( EchoSound, position );
		}
	}

	public float CooldownPercent => EchoCooldown > 0f ? MathF.Max( 0f, CurrentCooldown / EchoCooldown ) : 0f;
	public bool IsReady => CurrentCooldown <= 0f;
}
