namespace TrilobitCS.Responses;

// Laravel: ApiResource — definuje tvar JSON odpovědi
public record AuthResponse(string AccessToken, string RefreshToken);
