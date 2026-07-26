namespace AgentPlatform.Api.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    /// <summary>
    /// Mesaje explicite, nu un sablon: acordul in romana difera dupa genul
    /// substantivului, iar "Proprietatea nu a fost gasit" e vizibil greșit.
    /// </summary>
    public static NotFoundException Property() =>
        new("Proprietatea nu a fost găsită sau nu aparține contului tău.");

    public static NotFoundException AiAgent() =>
        new("Agentul AI nu a fost găsit sau nu aparține contului tău.");

    public static NotFoundException Lead() =>
        new("Leadul nu a fost găsit sau nu aparține contului tău.");

    public static NotFoundException Conversation() =>
        new("Conversația nu a fost găsită sau nu aparține contului tău.");

    public static NotFoundException VoiceAgent() =>
        new("Agentul vocal nu a fost găsit sau nu aparține contului tău.");

    public static NotFoundException VoiceCall() =>
        new("Apelul nu a fost găsit sau nu aparține contului tău.");

    public static NotFoundException Account() =>
        new("Contul nu a fost găsit.");
}
