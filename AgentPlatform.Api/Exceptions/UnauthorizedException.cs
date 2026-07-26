namespace AgentPlatform.Api.Exceptions;

/// <summary>
/// Nu exista in BCL, deci o definim noi. E aruncata cand tokenul a trecut
/// validarea dar nu contine claim-urile de care avem nevoie.
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}
