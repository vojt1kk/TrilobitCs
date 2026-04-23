using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Requests;

namespace TrilobitCS.Features.Auth;

public record LogoutCommand(RefreshRequest Request) : IRequest;

// Laravel: Auth::logout() — zneplatní token konkrétního zařízení
public class LogoutHandler : IRequestHandler<LogoutCommand>
{
    private readonly AppDbContext _db;

    public LogoutHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.Request.RefreshToken))
            throw new UnauthorizedException();

        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == command.Request.RefreshToken, cancellationToken)
            ?? throw new NotFoundException("errors.invalid_refresh_token");

        token.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
