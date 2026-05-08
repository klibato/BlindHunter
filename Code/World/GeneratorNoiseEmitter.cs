/// <summary>
/// Emits a continuous noise wave + 3D hum sound from an activated generator (Interactable.IsCompleted == true).
/// More active generators = more passive edge reveal for the killer = harder for survivors.
/// Add this component to every generator GameObject (alongside its Interactable).
/// </summary>
using System;
public sealed class GeneratorNoiseEmitter : Component
{
	[Property] public float NoiseIntensity { get; set; } = 300;
	[Property] public float NoiseInterval { get; set; } = 2.0f;

	[Property] public SoundEvent HumSound { get; set; }
	[Property] public float HumLoopDuration { get; set; } = 60f;

	private Interactable _interactable;
	private float _waveTimer;

	private SoundHandle _humHandle;
	private float _humReplayTimer;
	private bool _humPlaying;

	protected override void OnStart()
	{
		_interactable = GetComponent<Interactable>();
		// Stagger pour éviter que tous les gens émettent en synchro
		_waveTimer = Random.Shared.Float( 0f, NoiseInterval );
	}

	protected override void OnUpdate()
	{
		if ( _interactable == null ) return;

		if ( !_interactable.IsCompleted )
		{
			StopHum();
			return;
		}

		UpdateHum();
		UpdateWave();
	}

	private void UpdateWave()
	{
		// Seul le client local du killer pousse la noise (visuelle, drive le shader)
		var localPlayer = Scene.GetAllComponents<PlayerSetup>()
			.FirstOrDefault( p => !p.IsProxy );

		if ( localPlayer == null || localPlayer.Role != PlayerRole.Killer ) return;

		_waveTimer -= Time.Delta;
		if ( _waveTimer <= 0f )
		{
			NoiseVisualizer.AddNoise( WorldPosition, NoiseIntensity );
			_waveTimer = NoiseInterval;
		}
	}

	private void UpdateHum()
	{
		if ( HumSound == null ) return;

		if ( !_humPlaying )
		{
			PlayHum();
			return;
		}

		_humReplayTimer -= Time.Delta;
		if ( _humReplayTimer <= 0f )
		{
			PlayHum();
		}
	}

	private void PlayHum()
	{
		if ( HumSound == null ) return;

		StopHum();
		_humHandle = Sound.Play( HumSound, WorldPosition );
		_humReplayTimer = HumLoopDuration - 0.1f;
		_humPlaying = true;
	}

	private void StopHum()
	{
		if ( _humHandle.IsValid() )
		{
			_humHandle.Stop();
		}
		_humHandle = default;
		_humPlaying = false;
	}

	protected override void OnDisabled()
	{
		StopHum();
	}
}
