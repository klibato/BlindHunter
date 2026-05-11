using System;
using System.Linq;
using Sandbox; // Assure-toi d'avoir les bons namespaces pour S&box

/// <summary>Marks a GameObject as interactable by survivors. Can optionally count as a quest objective.</summary>
public sealed class Interactable : Component
{
    [Property] public string PromptText { get; set; } = "Press E to interact";
    [Property] public bool IsQuestObject { get; set; } = true;
    [Property] public float NoiseIntensity { get; set; } = 200f;
    
    // On expose la LED pour pouvoir la glisser-déposer dans l'éditeur
    [Property] public ModelRenderer LedRenderer { get; set; }

    [Sync( SyncFlags.FromHost )] public bool IsCompleted { get; set; }

    public Func<PlayerSetup, bool> CanInteract { get; set; }
    public event Action<PlayerSetup> OnInteracted;

    // Plus besoin de OnStart pour récupérer le composant localement
    
    public void Interact( PlayerSetup interactor )
    {
        if ( IsCompleted ) return;
        if ( !Networking.IsHost ) return;

        if ( CanInteract != null && !CanInteract( interactor ) )
        {
            Log.Info( $"[Interactable] {GameObject.Name}: CanInteract returned false for {interactor?.GameObject.Name ?? "null"}" );
            return;
        }

        IsCompleted = true;
        Log.Info( $"[Interactable] {GameObject.Name} completed by {interactor?.GameObject.Name ?? "null"}" );
        OnInteracted?.Invoke( interactor );

        EmitNoiseRpc( WorldPosition, NoiseIntensity );
    }

    [Rpc.Broadcast]
    private void EmitNoiseRpc( Vector3 position, float intensity )
    {
        var localPlayer = Scene.GetAllComponents<PlayerSetup>()
            .FirstOrDefault( p => !p.IsProxy );
            
        if ( localPlayer == null || localPlayer.Role != PlayerRole.Killer ) return;

        NoiseVisualizer.AddNoise( position, intensity );
    }

    protected override void OnUpdate()
    {
        if ( LedRenderer == null ) return;

        // Rouge si pas fait, Vert si terminé
        LedRenderer.Tint = IsCompleted ? Color.Green : Color.Red;
    }
}