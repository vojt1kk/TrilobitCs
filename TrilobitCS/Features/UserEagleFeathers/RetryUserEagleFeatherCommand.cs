using MediatR;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.UserEagleFeathers;

public record RetryUserEagleFeatherCommand(int UserId, int UserEagleFeatherId)
    : IRequest<UserEagleFeatherResponse>;

public class RetryUserEagleFeatherHandler : IRequestHandler<RetryUserEagleFeatherCommand, UserEagleFeatherResponse>
{
    private readonly AppDbContext _db;

    public RetryUserEagleFeatherHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserEagleFeatherResponse> Handle(RetryUserEagleFeatherCommand command, CancellationToken cancellationToken)
    {
        var uef = await _db.UserEagleFeathers.FindAsync([command.UserEagleFeatherId], cancellationToken)
            ?? throw new NotFoundException("errors.user_eagle_feather_not_found");

        if (uef.UserId != command.UserId)
            throw new ForbiddenException("errors.forbidden");

        if (uef.Status != EagleFeatherStatus.Rejected)
            throw new ConflictException("errors.user_eagle_feather_cannot_retry");

        uef.Status = EagleFeatherStatus.Pending;
        uef.VerifiedById = null;
        uef.EarnedAt = null;
        uef.ModeratorNote = null;
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
