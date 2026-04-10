using MediatR;
using TrilobitCS.Exceptions;
using TrilobitCS.Repositories;
using TrilobitCS.Requests;

namespace TrilobitCS.Features.Auth;

public record LogoutCommand(RefreshRequest Request) : IRequest;

// Laravel: Auth::logout() — zneplatní token konkrétního zařízení
public class LogoutHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var token = await _refreshTokenRepository.FindWithUser(command.Request.RefreshToken, cancellationToken)
            ?? throw new NotFoundException("errors.invalid_refresh_token");

        token.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.Save(cancellationToken);
    }
}
