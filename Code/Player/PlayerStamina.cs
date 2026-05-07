using System;

/// <summary>
/// Manages survivor stamina. Running consumes stamina, walking/idle regenerates.
/// Caps the player's max speed when stamina is empty.
/// </summary>
public sealed class PlayerStamina : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }
	[Property] public float MaxStamina { get; set; } = 100f;
	[Property] public float RunDrainRate { get; set; } = 25f;
	[Property] public float RegenRate { get; set; } = 15f;
	[Property] public float RegenDelayAfterUse { get; set; } = 1f;
	[Property] public float MinStaminaToRun { get; set; } = 10f;
	[Property] public float WalkSpeed { get; set; } = 100f;
	[Property] public float RunSpeed { get; set; } = 220f;

	[Sync] public float CurrentStamina { get; set; }
	[Sync] public bool IsRunning { get; set; }
	[Sync] public bool CanRun { get; set; } = true;

	private float _lastDrainTime;
	private PlayerController _controller;

	protected override void OnStart()
	{
		_controller = GetComponent<PlayerController>();
		CurrentStamina = MaxStamina;
	}

	protected override void OnUpdate()
	{
		if ( TargetPlayer == null || TargetPlayer.IsProxy ) return;
		if ( !TargetPlayer.IsAlive ) return;
		if ( _controller == null ) return;

		bool tryingToRun = Input.Down( "Run" );
		float speed = _controller.Velocity.Length;
		bool actuallyRunning = tryingToRun && speed > 100f && CanRun;

		IsRunning = actuallyRunning;

		if ( actuallyRunning )
		{
			CurrentStamina -= RunDrainRate * Time.Delta;
			_lastDrainTime = Time.Now;

			if ( CurrentStamina <= 0f )
			{
				CurrentStamina = 0f;
				CanRun = false;
			}
		}
		else
		{
			if ( Time.Now - _lastDrainTime > RegenDelayAfterUse )
			{
				CurrentStamina += RegenRate * Time.Delta;
				CurrentStamina = MathF.Min( CurrentStamina, MaxStamina );

				if ( !CanRun && CurrentStamina >= MinStaminaToRun )
				{
					CanRun = true;
				}
			}
		}

		ApplySpeedLimit();
	}

	private void ApplySpeedLimit()
	{
		// Bride la vitesse de course du PlayerController selon stamina
		// Si CanRun = false → on cap à WalkSpeed même si Run est pressé
		if ( !CanRun )
		{
			_controller.RunSpeed = WalkSpeed;
		}
		else
		{
			_controller.RunSpeed = RunSpeed;
		}
	}

	public float StaminaPercent => MaxStamina > 0f ? CurrentStamina / MaxStamina : 0f;
}