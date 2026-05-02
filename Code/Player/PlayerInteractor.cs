using System;

/// <summary>Casts a ray from the survivor's camera and handles interaction with <see cref="Interactable"/> objects.</summary>
public sealed class PlayerInteractor : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }
	[Property] public float InteractionRange { get; set; } = 150f;

	public Interactable CurrentTarget { get; private set; }

	private CameraComponent _camera;

	protected override void OnStart()
	{
		_camera = GetComponentInChildren<CameraComponent>();
	}

	protected override void OnUpdate()
	{
		if ( TargetPlayer == null || TargetPlayer.IsProxy )
			return;

		if ( TargetPlayer.Role != PlayerRole.Survivor )
			return;

		if ( _camera == null )
			return;

		var ray = new Ray( _camera.WorldPosition, _camera.WorldRotation.Forward );
		var trace = Scene.Trace
			.Ray( ray, InteractionRange )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( trace.Hit && trace.GameObject != null )
		{
			var interactable = trace.GameObject.GetComponent<Interactable>();

			if ( interactable != null && !interactable.IsCompleted )
			{
				CurrentTarget = interactable;

				if ( Input.Pressed( "Use" ) )
					InteractRpc( interactable.GameObject.Id );

				return;
			}
		}

		CurrentTarget = null;
	}

	[Rpc.Broadcast]
	private void InteractRpc( Guid interactableId )
	{
		if ( !Networking.IsHost ) return;

		var go = Scene.Directory.FindByGuid( interactableId );
		if ( go == null ) return;

		var interactable = go.GetComponent<Interactable>();
		if ( interactable == null ) return;

		interactable.Interact( TargetPlayer );
	}
}
