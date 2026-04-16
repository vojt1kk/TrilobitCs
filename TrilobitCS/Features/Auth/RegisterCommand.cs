using MediatR;
using TrilobitCS.Auth;
using TrilobitCS.Dto;
using TrilobitCS.Exceptions;
using TrilobitCS.Repositories;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Auth;

public record RegisterCommand(RegisterRequest Request) : IRequest<AuthResponse>;

// Laravel: AuthController@register
public class RegisterHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly BcryptPasswordHasher _hasher;
    private readonly JwtTokenService _jwtTokenService;

    public RegisterHandler(
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

    public async Task<AuthResponse> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (await _userRepository.EmailExists(request.Email, cancellationToken))
            throw new ConflictException("errors.email_taken");

        if (await _userRepository.NicknameExists(request.Nickname, cancellationToken))
            throw new ConflictException("errors.nickname_taken");

        var user = await _userRepository.Create(
            CreateUserDto.FromRequest(request, _hasher.Hash(request.Password)),
            cancellationToken);

        var refreshToken = await _refreshTokenRepository.Create(
            _jwtTokenService.GenerateRefreshToken(user), cancellationToken);

        return new AuthResponse(
            AccessToken: _jwtTokenService.GenerateAccessToken(user),
            RefreshToken: refreshToken.Token
        );
    }
}
