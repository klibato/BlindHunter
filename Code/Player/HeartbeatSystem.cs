using System.Linq;

public sealed class HeartbeatSystem : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }

	[Property] public float CloseDistance { get; set; } = 800f;
	[Property] public float MediumDistance { get; set; } = 1500f;
    [Property] public float SafeDistance { get; set; } = 2500f; // ← nouveau

	[Property] public SoundEvent HeartbeatFar { get; set; }
	[Property] public SoundEvent HeartbeatMedium { get; set; }
	[Property] public SoundEvent HeartbeatClose { get; set; }

	// Durée approximative de tes wav, pour savoir quand relancer.
	// Tu peux ajuster ces valeurs dans l'inspector si nécessaire.
	[Property] public float FarLoopDuration { get; set; } = 30f;
	[Property] public float MediumLoopDuration { get; set; } = 15.4f;
	[Property] public float CloseLoopDuration { get; set; } = 14f;

	public HeartbeatZone CurrentZone { get; private set; } = HeartbeatZone.None;

	private SoundHandle _currentSound;
	private HeartbeatZone _playingZone = HeartbeatZone.None;
	private float _replayTimer;

	protected override void OnUpdate()
	{
		if ( TargetPlayer == null || TargetPlayer.IsProxy )
		{
			SetZone( HeartbeatZone.None );
			return;
		}

		if ( GameManager.Instance == null || GameManager.Instance.State != LobbyState.InGame )
		{
			SetZone( HeartbeatZone.None );
			return;
		}

		if ( TargetPlayer.Role != PlayerRole.Survivor || !TargetPlayer.IsAlive )
		{
			SetZone( HeartbeatZone.None );
			return;
		}

		var killer = FindKiller();
		if ( killer == null )
		{
			SetZone( HeartbeatZone.None );
			return;
		}

		float distance = Vector3.DistanceBetween( TargetPlayer.WorldPosition, killer.WorldPosition );
		SetZone( ResolveZone( distance ) );

		// Si on a un son qui joue, on track sa durée pour le relancer en boucle
		if ( CurrentZone != HeartbeatZone.None )
		{
			_replayTimer -= Time.Delta;
			if ( _replayTimer <= 0f )
			{
				PlayCurrentZoneSound();
			}
		}
	}

	private void SetZone( HeartbeatZone newZone )
	{
		CurrentZone = newZone;

		// Changement de zone → on stop le son en cours et on relance le nouveau
		if ( newZone == _playingZone ) return;

		StopCurrentSound();
		_playingZone = newZone;

		if ( newZone != HeartbeatZone.None )
		{
			PlayCurrentZoneSound();
		}
	}

	private void PlayCurrentZoneSound()
	{
		var sound = CurrentZone switch
		{
			HeartbeatZone.Close => HeartbeatClose,
			HeartbeatZone.Medium => HeartbeatMedium,
			HeartbeatZone.Far => HeartbeatFar,
			_ => null
		};

		float duration = CurrentZone switch
		{
			HeartbeatZone.Close => CloseLoopDuration,
			HeartbeatZone.Medium => MediumLoopDuration,
			HeartbeatZone.Far => FarLoopDuration,
			_ => 1f
		};

		// Stop l'ancien avant de relancer (au cas où)
		StopCurrentSound();

		if ( sound != null )
		{
			_currentSound = Sound.Play( sound );
		}

		// On programme le replay légèrement avant la fin pour éviter un trou
		_replayTimer = duration - 0.1f;
	}

	private void StopCurrentSound()
	{
		if ( _currentSound.IsValid() )
		{
			_currentSound.Stop();
		}
		_currentSound = default;
	}

	protected override void OnDisabled()
	{
		StopCurrentSound();
		_playingZone = HeartbeatZone.None;
	}

	private HeartbeatZone ResolveZone( float distance )
	{
		if ( distance < CloseDistance ) return HeartbeatZone.Close;
		if ( distance < MediumDistance ) return HeartbeatZone.Medium;
        if ( distance < SafeDistance ) return HeartbeatZone.Far;
		return HeartbeatZone.None;
	}

	private PlayerSetup FindKiller()
	{
		return Scene.GetAllComponents<PlayerSetup>()
			.FirstOrDefault( p => p.Role == PlayerRole.Killer && p.IsAlive );
	}
}

public enum HeartbeatZone
{
	None,
	Far,
	Medium,
	Close
}