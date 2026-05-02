/// <summary>Controls the survivor's spotlight, toggled with the Flashlight input action.</summary>
public sealed class Flashlight : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }

	private SpotLight _spotLight;
	private bool _isOn;

	protected override void OnStart()
	{
		_spotLight = GetComponent<SpotLight>();
	}

	protected override void OnUpdate()
	{
		if ( _spotLight == null ) return;
		if ( TargetPlayer == null || TargetPlayer.IsProxy ) return;

		// Killer has no flashlight.
		if ( TargetPlayer.Role == PlayerRole.Killer )
		{
			_spotLight.Enabled = false;
			return;
		}

		if ( Input.Pressed( "Flashlight" ) )
			_isOn = !_isOn;

		_spotLight.Enabled = _isOn;
	}
}
