public sealed class Flashlight : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }
	[Property] public ModelRenderer ViewModel { get; set; }
	[Property] public ModelRenderer WorldModel { get; set; }

	private SpotLight _spotLight;

	[Sync] public bool IsOn { get; set; }

	protected override void OnStart()
	{
		_spotLight = GetComponent<SpotLight>();
	}

	protected override void OnUpdate()
	{
		if ( TargetPlayer == null ) return;
		if ( _spotLight == null ) return;

		if ( TargetPlayer.Role == PlayerRole.Killer )
		{
			_spotLight.Enabled = false;
			if ( ViewModel != null ) ViewModel.Enabled = false;
			if ( WorldModel != null ) WorldModel.Enabled = false;
			return;
		}

		if ( !TargetPlayer.IsProxy && Input.Pressed( "Flashlight" ) )
		{
			IsOn = !IsOn;
		}

		WorldRotation = TargetPlayer.EyeRotation;
		_spotLight.Enabled = IsOn;

		if ( ViewModel != null ) ViewModel.Enabled = !TargetPlayer.IsProxy;
		if ( WorldModel != null ) WorldModel.Enabled = TargetPlayer.IsProxy;
	}
}