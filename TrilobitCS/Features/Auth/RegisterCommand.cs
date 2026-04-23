using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Auth;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Auth;

public record RegisterCommand(RegisterRequest Request) : IRequest<AuthResponse>;

// Laravel: AuthController@register
public class RegisterHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly AppDbContext _db;
    private readonly BcryptPasswordHasher _hasher;
    private readonly JwtTokenService _jwtTokenService;

    public RegisterHandler(AppDbContext db, BcryptPasswordHasher hasher, JwtTokenService jwtTokenService)
    {
        _db = db;
        _hasher = hasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (await _db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            throw new ConflictException("errors.email_taken");

        if (await _db.Users.AnyAsync(u => u.Nickname == request.Nickname, cancellationToken))
            throw new ConflictException("errors.nickname_taken");

        var user = new User
        {
            Nickname = request.Nickname,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Password = _hasher.Hash(request.Password),
            Gender = request.Gender,
            BirthDate = request.BirthDate,
            CreatedAt = DateTime.UtcNow,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var refreshToken = _jwtTokenService.GenerateRefreshToken(user);
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            AccessToken: _jwtTokenService.GenerateAccessToken(user),
            RefreshToken: refreshToken.Token
        );
    }
}
