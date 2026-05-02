using Sandbox;
using System;

public sealed class Interactable : Component
{
    [Property] public string PromptText { get; set; } = "Press E to interact";
    [Property] public bool IsQuestObject { get; set; } = true;
    [Sync(SyncFlags.FromHost)] public bool IsCompleted { get; set; }

    public event Action<PlayerSetup> OnInteracted;

    public void Interact(PlayerSetup interactor)
    {
        if (IsCompleted) return;
        if (!Networking.IsHost) return;

        IsCompleted = true;
        OnInteracted?.Invoke(interactor);
        Log.Info($"Interactable {GameObject.Name} was activated by {interactor.GameObject.Name}");
    }

    protected override void OnUpdate()
    {
        var renderer = GetComponent<ModelRenderer>();
        if (renderer == null)
        {
            return;
        }

        renderer.Tint = IsCompleted ? Color.Green : Color.Yellow;
    }
}