using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.UserEagleFeathers;

public record CreateUserEagleFeatherCommand(int UserId, CreateUserEagleFeatherRequest Request)
    : IRequest<UserEagleFeatherResponse>;

public class CreateUserEagleFeatherHandler : IRequestHandler<CreateUserEagleFeatherCommand, UserEagleFeatherResponse>
{
    private readonly AppDbContext _db;

    public CreateUserEagleFeatherHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserEagleFeatherResponse> Handle(CreateUserEagleFeatherCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (!await _db.EagleFeathers.AnyAsync(ef => ef.Id == request.EagleFeatherId, cancellationToken))
            throw new NotFoundException("errors.eagle_feather_not_found");

        if (await _db.UserEagleFeathers.AnyAsync(
                uef => uef.UserId == command.UserId && uef.EagleFeatherId == request.EagleFeatherId,
                cancellationToken))
            throw new ConflictException("errors.user_eagle_feather_already_exists");

        var uef = new UserEagleFeather
        {
            UserId = command.UserId,
            EagleFeatherId = request.EagleFeatherId,
            IsGrandChallenge = request.IsGrandChallenge,
            Status = EagleFeatherStatus.Pending,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
        };

        _db.UserEagleFeathers.Add(uef);
        await _db.SaveChangesAsync(cancellationToken);

        return new UserEagleFeatherResponse(
            uef.Id,
            uef.UserId,
            uef.EagleFeatherId,
            uef.IsGrandChallenge,
            uef.IsCompleted,
            uef.Status,
            uef.VerifiedById,
            uef.ModeratorNote,
            uef.EarnedAt,
            uef.CreatedAt
        );
    }
}
