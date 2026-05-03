/// <summary>Controls the survivor's spotlight, view-model and world-model. Toggled with the Flashlight input action.</summary>
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
		if (TargetPlayer == null) return;
		if (_spotLight == null) return;

		// Killer has no flashlight at all
		if (TargetPlayer.Role == PlayerRole.Killer)
		{
			_spotLight.Enabled = false;
			if (ViewModel != null) ViewModel.Enabled = false;
			if (WorldModel != null) WorldModel.Enabled = false;
			return;
		}

		// Toggle handled by local player only
		if (!TargetPlayer.IsProxy && Input.Pressed("Flashlight"))
		{
			IsOn = !IsOn;
		}

		// Apply EyeRotation so the cone follows the head direction across all clients
		WorldRotation = TargetPlayer.EyeRotation;

		// SpotLight visible to everyone when ON
		_spotLight.Enabled = IsOn;

		// View-model : visible uniquement par le joueur local (toujours, lampe allumée ou non)
		if (ViewModel != null)
		{
			ViewModel.Enabled = !TargetPlayer.IsProxy;
		}

		// World-model : visible uniquement par les autres (proxy)
		if (WorldModel != null)
		{
			WorldModel.Enabled = TargetPlayer.IsProxy;
		}
	}
}