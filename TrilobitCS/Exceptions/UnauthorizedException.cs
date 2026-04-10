namespace TrilobitCS.Exceptions;

// Laravel: AuthenticationException → vrací 401
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Unauthorized") : base(message) { }
}
