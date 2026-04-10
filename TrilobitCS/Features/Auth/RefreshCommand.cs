using MediatR;
using TrilobitCS.Auth;
using TrilobitCS.Exceptions;
using TrilobitCS.Repositories;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Auth;

public record RefreshCommand(RefreshRequest Request) : IRequest<AuthResponse>;

// Vymění platný refresh token za nový access token + nový refresh token
// (rotation — každý refresh token lze použít jen jednou)
public class RefreshHandler : IRequestHandler<RefreshCommand, AuthResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly JwtTokenService _jwtTokenService;

    public RefreshHandler(IRefreshTokenRepository refreshTokenRepository, JwtTokenService jwtTokenService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> Handle(RefreshCommand command, CancellationToken cancellationToken)
    {
        var token = await _refreshTokenRepository.FindWithUser(command.Request.RefreshToken, cancellationToken);

        if (token == null || !token.IsValid)
            throw new UnauthorizedException("errors.invalid_refresh_token");

        // Rotation — starý token zneplatni, vygeneruj nový
        token.RevokedAt = DateTime.UtcNow;

        var newRefreshToken = await _refreshTokenRepository.Create(
            _jwtTokenService.GenerateRefreshToken(token.User), cancellationToken);

        await _refreshTokenRepository.Save(cancellationToken);

        return new AuthResponse(
            AccessToken: _jwtTokenService.GenerateAccessToken(token.User),
            RefreshToken: newRefreshToken.Token
        );
    }
}
