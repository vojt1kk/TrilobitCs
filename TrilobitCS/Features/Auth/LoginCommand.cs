using MediatR;
using TrilobitCS.Auth;
using TrilobitCS.Exceptions;
using TrilobitCS.Repositories;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Auth;

public record LoginCommand(LoginRequest Request) : IRequest<AuthResponse>;

// Laravel: AuthController@login
public class LoginHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly BcryptPasswordHasher _hasher;
    private readonly JwtTokenService _jwtTokenService;

    public LoginHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        BcryptPasswordHasher hasher,
        JwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _hasher = hasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var user = await _userRepository.FindByNickname(request.Nickname, cancellationToken);

        if (user == null || !_hasher.Verify(request.Password, user.Password))
            throw new UnauthorizedException("errors.invalid_credentials");

        var refreshToken = await _refreshTokenRepository.Create(
            _jwtTokenService.GenerateRefreshToken(user), cancellationToken);

        return new AuthResponse(
            AccessToken: _jwtTokenService.GenerateAccessToken(user),
            RefreshToken: refreshToken.Token
        );
    }
}
