namespace AgentPlatform.Api.Exceptions;

public class ValidationException : Exception
{
    /// <summary>Detalii per camp, expuse in raspunsul 400.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string message, IReadOnlyDictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }

    public static ValidationException ForField(string field, string message) =>
        new(message, new Dictionary<string, string[]> { [field] = [message] });
}
