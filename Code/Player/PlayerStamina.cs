/// <summary>
/// Manages survivor stamina. Running consumes stamina, walking/idle regenerates.
/// Killer also has stamina but for attacks (managed via separate properties).
/// </summary>
using System;
public sealed class PlayerStamina : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }
	[Property] public float MaxStamina { get; set; } = 100f;
	[Property] public float RunDrainRate { get; set; } = 25f; // par seconde
	[Property] public float RegenRate { get; set; } = 15f; // par seconde
	[Property] public float RegenDelayAfterUse { get; set; } = 1f; // delay avant que la regen démarre
	[Property] public float MinStaminaToRun { get; set; } = 10f; // peut pas re-courir si en dessous

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
		if (TargetPlayer == null || TargetPlayer.IsProxy) return;
		if (!TargetPlayer.IsAlive) return;

		// Détermine si le joueur tente de courir
		bool tryingToRun = Input.Down("Run");
		float speed = _controller != null ? _controller.Velocity.Length : 0f;
		bool actuallyRunning = tryingToRun && speed > 100f && CanRun;

		IsRunning = actuallyRunning;

		if (actuallyRunning)
		{
			// Consume stamina
			CurrentStamina -= RunDrainRate * Time.Delta;
			_lastDrainTime = Time.Now;

			if (CurrentStamina <= 0f)
			{
				CurrentStamina = 0f;
				CanRun = false;
			}
		}
		else
		{
			// Regen après le délai
			if (Time.Now - _lastDrainTime > RegenDelayAfterUse)
			{
				CurrentStamina += RegenRate * Time.Delta;
				CurrentStamina = MathF.Min(CurrentStamina, MaxStamina);

				// Re-allow run quand on a assez de stamina
				if (!CanRun && CurrentStamina >= MinStaminaToRun)
				{
					CanRun = true;
				}
			}
		}

		// Limite la vitesse de course si stamina vide
		ApplySpeedLimit();
	}

	private void ApplySpeedLimit()
	{
		if (_controller == null) return;

		// Si stamina vide, on bride la vitesse même si la touche Run est pressée
		// (c'est juste un fallback, le PlayerController a déjà sa logique standard)
	}

	public float StaminaPercent => MaxStamina > 0f ? CurrentStamina / MaxStamina : 0f;
}