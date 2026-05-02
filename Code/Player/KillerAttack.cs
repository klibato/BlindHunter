using System;

/// <summary>
/// Component qui gère l'attaque du tueur en mêlée.
/// </summary>
public sealed class KillerAttack : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }
	[Property] public float AttackRange { get; set; } = 100f;
	[Property] public float AttackCooldown { get; set; } = 1f;

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

		if ( !trace.Hit || trace.GameObject == null )
			return;

		// On cherche un PlayerSetup sur le GameObject touché ou ses parents
		var hitPlayer = trace.GameObject.GetComponentInParent<PlayerSetup>();
		if ( hitPlayer == null )
			return;

		if ( hitPlayer.Role != PlayerRole.Survivor )
			return;

		if ( !hitPlayer.IsAlive )
			return;

		KillRpc( hitPlayer.GameObject.Id );
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