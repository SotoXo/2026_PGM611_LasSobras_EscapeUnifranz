namespace EscapeUNIFRANZ.Interaction
{
    /// <summary>
    /// Contract for a world object that can receive the player's contextual interaction.
    /// </summary>
    public interface IInteractable
    {
        string Prompt { get; }
        int Priority { get; }
        bool CanInteract { get; }

        void Interact();
    }
}
