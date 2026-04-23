using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Auth;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Auth;

public record LoginCommand(LoginRequest Request) : IRequest<AuthResponse>;

// Laravel: AuthController@login
public class LoginHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly AppDbContext _db;
    private readonly BcryptPasswordHasher _hasher;
    private readonly JwtTokenService _jwtTokenService;

    public LoginHandler(AppDbContext db, BcryptPasswordHasher hasher, JwtTokenService jwtTokenService)
    {
        _db = db;
        _hasher = hasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Nickname == request.Nickname, cancellationToken);

        if (user == null || !_hasher.Verify(request.Password, user.Password))
            throw new UnauthorizedException("errors.invalid_credentials");

        var refreshToken = _jwtTokenService.GenerateRefreshToken(user);
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            AccessToken: _jwtTokenService.GenerateAccessToken(user),
            RefreshToken: refreshToken.Token
        );
    }
}
