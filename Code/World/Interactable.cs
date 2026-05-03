using System;

/// <summary>Marks a GameObject as interactable by survivors. Can optionally count as a quest objective.</summary>
public sealed class Interactable : Component
{
	[Property] public string PromptText { get; set; } = "Press E to interact";
	[Property] public bool IsQuestObject { get; set; } = true;
	[Sync( SyncFlags.FromHost )] public bool IsCompleted { get; set; }

	/// <summary>
	/// Callback de validation custom. Si set, retourne false pour bloquer l'interaction silencieusement.
	/// </summary>
	public Func<PlayerSetup, bool> CanInteract { get; set; }

	public event Action<PlayerSetup> OnInteracted;

	private ModelRenderer _renderer;

	protected override void OnStart()
	{
		_renderer = GetComponent<ModelRenderer>();
	}

	/// <summary>Completes this object and fires <see cref="OnInteracted"/>. Host only.</summary>
	public void Interact( PlayerSetup interactor )
	{
		if ( IsCompleted ) return;
		if ( !Networking.IsHost ) return;

		// Validation custom : si le callback dit non, on bloque l'interaction silencieusement
		if ( CanInteract != null && !CanInteract( interactor ) )
		{
			return;
		}

		IsCompleted = true;
		OnInteracted?.Invoke( interactor );
	}

	protected override void OnUpdate()
	{
		if ( _renderer == null ) return;
		_renderer.Tint = IsCompleted ? Color.Green : Color.Yellow;
	}
}