using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Auth;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Auth;

public record RefreshCommand(RefreshRequest Request) : IRequest<AuthResponse>;

// Vymění platný refresh token za nový access token + nový refresh token
// (rotation — každý refresh token lze použít jen jednou)
public class RefreshHandler : IRequestHandler<RefreshCommand, AuthResponse>
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _jwtTokenService;

    public RefreshHandler(AppDbContext db, JwtTokenService jwtTokenService)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> Handle(RefreshCommand command, CancellationToken cancellationToken)
    {
        var token = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == command.Request.RefreshToken, cancellationToken);

        if (token == null || !token.IsValid)
            throw new UnauthorizedException("errors.invalid_refresh_token");

        // Rotation — starý token zneplatni, vygeneruj nový
        token.RevokedAt = DateTime.UtcNow;

        var newRefreshToken = _jwtTokenService.GenerateRefreshToken(token.User);
        _db.RefreshTokens.Add(newRefreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            AccessToken: _jwtTokenService.GenerateAccessToken(token.User),
            RefreshToken: newRefreshToken.Token
        );
    }
}
