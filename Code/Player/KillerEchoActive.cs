using System.Linq;
using System;

/// <summary>
/// Active echolocation for the killer : pure wave emission with a cooldown.
/// Detached from KillerAttack so the killer can scan without swinging
/// (avoids spamming the miss whoosh just to "see").
/// The echo sound fades out in sync with the visual wave decay.
/// </summary>
public sealed class KillerEchoActive : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }
	[Property] public float EchoNoiseIntensity { get; set; } = 250f;
	[Property] public float EchoCooldown { get; set; } = 1.5f;
	[Property] public SoundEvent EchoSound { get; set; }

	// Durée du fade out audio. Doit matcher la durée de vie de la wave visuelle
	// (NoiseLifetime = 2.5s dans NoiseVisualizer). Tunable dans l'inspector.
	[Property] public float SoundFadeDuration { get; set; } = 2.5f;

	[Sync] public float CurrentCooldown { get; set; }

	// État local du fade (pas synchronisé : chaque client gère son propre handle audio)
	private SoundHandle _localEchoHandle;
	private float _fadeStartTime;
	private bool _isFading;

	protected override void OnUpdate()
	{
		// Fade tourne sur tous les clients (owner + proxies) car chaque client
		// a sa propre instance de SoundHandle locale après le Sound.Play()
		UpdateSoundFade();

		// Input handling uniquement côté owner local
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

	private void UpdateSoundFade()
	{
		if ( !_isFading ) return;
		if ( !_localEchoHandle.IsValid() )
		{
			_isFading = false;
			return;
		}

		float elapsed = Time.Now - _fadeStartTime;
		float t = SoundFadeDuration > 0f ? elapsed / SoundFadeDuration : 1f;

		if ( t >= 1f )
		{
			_localEchoHandle.Stop();
			_localEchoHandle = default;
			_isFading = false;
			return;
		}

		// Fade linéaire : commence à plein volume, descend en continu jusqu'à 0
		// quand la wave visuelle s'éteint. Si tu veux que le son reste fort plus
		// longtemps puis fade brutalement, remplace par une curve cube :
		// _localEchoHandle.Volume = 1f - (t * t * t);
		_localEchoHandle.Volume = 1f - t;
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
		if ( EchoSound == null ) return;

		// Stop le précédent si rapid-fire (évite l'empilement de sons)
		if ( _localEchoHandle.IsValid() )
		{
			_localEchoHandle.Stop();
		}

		_localEchoHandle = Sound.Play( EchoSound, position );
		_fadeStartTime = Time.Now;
		_isFading = true;
	}

	protected override void OnDisabled()
	{
		if ( _localEchoHandle.IsValid() )
		{
			_localEchoHandle.Stop();
		}
		_localEchoHandle = default;
		_isFading = false;
	}

	public float CooldownPercent => EchoCooldown > 0f ? MathF.Max( 0f, CurrentCooldown / EchoCooldown ) : 0f;
	public bool IsReady => CurrentCooldown <= 0f;
}
