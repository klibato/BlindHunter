public sealed class TestMover : Component
{
	[Property] public float Speed { get; set; } = 100000f;

	protected override void OnUpdate()
	{
		Vector3 direction = Vector3.Zero;

		if ( Input.Down( "Forward" ) )
			direction += Vector3.Forward;

		if ( Input.Down( "Backward" ) )
			direction += Vector3.Backward;

		if ( Input.Down( "Left" ) )
			direction += Vector3.Left;

		if ( Input.Down( "Right" ) )
			direction += Vector3.Right;

		WorldPosition += direction * Speed * Time.Delta* 5f;
	}
}