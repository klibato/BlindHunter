using Sandbox;

public sealed class Flashlight : Component
{
	[Property] public PlayerSetup TargetPlayer { get; set; }

	private SpotLight _spotLight;
	private bool _isOn;

	protected override void OnStart()
	{
		// Le component spotlight est sur le même GameObject (qu'on va ajouter)
		_spotLight = GetComponent<SpotLight>();

		// Le tueur n'a pas de lampe — désactivons le component pour lui
		// Mais cette vérif se fait en OnUpdate parce que Role peut être None à OnStart
	}

	protected override void OnUpdate()
	{
		if ( _spotLight == null )
			return;

		// Seul le joueur local contrôle sa propre lampe
		if ( TargetPlayer == null || TargetPlayer.IsProxy )
			return;

		// Le tueur n'a pas de lampe
		if ( TargetPlayer.Role == PlayerRole.Killer )
		{
			_spotLight.Enabled = false;
			return;
		}

		// Toggle avec F
		if ( Input.Pressed( "Flashlight" ) )
		{
			_isOn = !_isOn;
		}

		_spotLight.Enabled = _isOn;
	}
}