using System.Linq;
using System;
using System.Collections.Generic;

public sealed class SpectatorCamera : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }
	[Property] public float HeadHeightOffset { get; set; } = 64f;

	[Sync] public Guid CurrentTargetId { get; set; }

	private CameraComponent _camera;
	private bool _wasAlive = true;
	private SkinnedModelRenderer _hiddenRenderer;

	protected override void OnStart()
	{
		_camera = GetComponentInChildren<CameraComponent>();
	}

	protected override void OnUpdate()
	{
		if ( TargetPlayer == null || TargetPlayer.IsProxy ) return;
		if ( TargetPlayer.Role != PlayerRole.Survivor ) return;
		if ( _camera == null ) return;

		// Transition alive → dead
		if ( _wasAlive && !TargetPlayer.IsAlive )
		{
			_wasAlive = false;
			ChooseInitialTarget();
		}

		// Si vivant, on désactive le mode spectateur et on restaure le body si caché
		if ( TargetPlayer.IsAlive )
		{
			_wasAlive = true;
			RestoreHiddenBody();
			return;
		}

		if ( Input.Pressed( "Use" ) )
		{
			CycleNextTarget();
		}

		FollowTarget();
		HideTargetBody();
	}

	private void ChooseInitialTarget()
	{
		var alive = GetAliveSurvivors();
		if ( alive.Count == 0 ) return;
		CurrentTargetId = alive[0].GameObject.Id;
	}

	private void CycleNextTarget()
	{
		var alive = GetAliveSurvivors();
		if ( alive.Count == 0 ) return;

		// Restaure l'ancien body avant de switcher
		RestoreHiddenBody();

		int currentIndex = alive.FindIndex( p => p.GameObject.Id == CurrentTargetId );
		int nextIndex = ( currentIndex + 1 ) % alive.Count;
		CurrentTargetId = alive[nextIndex].GameObject.Id;
	}

	private void FollowTarget()
	{
		var target = GetTargetPlayer();
		if ( target == null ) return;

		Vector3 headOffset = new Vector3( 0, 0, HeadHeightOffset );
		_camera.WorldPosition = target.WorldPosition + headOffset;
		_camera.WorldRotation = target.EyeRotation;
	}

	private void HideTargetBody()
	{
		var target = GetTargetPlayer();
		if ( target == null ) return;

		var renderer = target.GameObject.GetComponentInChildren<SkinnedModelRenderer>();
		if ( renderer == null ) return;

		// Si on a changé de cible, restaure l'ancien
		if ( _hiddenRenderer != renderer )
		{
			RestoreHiddenBody();
			_hiddenRenderer = renderer;
		}

		renderer.Enabled = false;
	}

	private void RestoreHiddenBody()
	{
		if ( _hiddenRenderer != null )
		{
			_hiddenRenderer.Enabled = true;
			_hiddenRenderer = null;
		}
	}

	protected override void OnDisabled()
	{
		RestoreHiddenBody();
	}

	private PlayerSetup GetTargetPlayer()
	{
		var go = Scene.Directory.FindByGuid( CurrentTargetId );
		return go?.GetComponent<PlayerSetup>();
	}

	private List<PlayerSetup> GetAliveSurvivors()
	{
		return Scene.GetAllComponents<PlayerSetup>()
			.Where( p => p.Role == PlayerRole.Survivor && p.IsAlive )
			.ToList();
	}

	public string GetCurrentTargetName()
	{
		var target = GetTargetPlayer();
		return target?.GameObject.Name ?? "Unknown";
	}

	public bool IsSpectating => TargetPlayer != null && !TargetPlayer.IsAlive;
}