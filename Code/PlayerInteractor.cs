using Sandbox;
using System;
public sealed class PlayerInteractor : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }
	[Property] public float InteractionRange { get; set; } = 150f;

	public Interactable CurrentTarget { get; private set; }

	protected override void OnUpdate()
{
	if ( TargetPlayer == null || TargetPlayer.IsProxy )
		return;

	if ( TargetPlayer.Role != PlayerRole.Survivor )
		return;

	var camera = GetComponentInChildren<CameraComponent>();
	if ( camera == null )
	{
		Log.Info( "PlayerInteractor: no camera found" );
		return;
	}

	var ray = new Ray( camera.WorldPosition, camera.WorldRotation.Forward );
	var trace = Scene.Trace
		.Ray( ray, InteractionRange )
		.IgnoreGameObjectHierarchy( GameObject )
		.Run();

	// LOG DEBUG
	Log.Info( $"Trace hit: {trace.Hit}, GameObject: {trace.GameObject?.Name ?? "null"}, Distance: {trace.Distance}" );

	if ( trace.Hit && trace.GameObject != null )
	{
		var interactable = trace.GameObject.GetComponent<Interactable>();
		Log.Info( $"Interactable found: {interactable != null}" );

		if ( interactable != null && !interactable.IsCompleted )
		{
			CurrentTarget = interactable;

			if ( Input.Pressed( "Use" ) )
			{
				Log.Info( $"E pressed, calling RPC for {interactable.GameObject.Name}" );
				InteractRpc( interactable.GameObject.Id );
			}
			return;
		}
	}

	CurrentTarget = null;
}
	[Rpc.Broadcast]
	private void InteractRpc( Guid interactableId )
	{
		// Cette méthode tourne sur tous les clients
		// Mais seul le host fait l'action (autorité)
		if ( !Networking.IsHost ) return;

		var go = Scene.Directory.FindByGuid( interactableId );
		if ( go == null ) return;

		var interactable = go.GetComponent<Interactable>();
		if ( interactable == null ) return;

		interactable.Interact( TargetPlayer );
	}
}