using MediatR;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.UserEagleFeathers;

public record ApproveUserEagleFeatherCommand(int LeaderId, int UserEagleFeatherId, ModerateUserEagleFeatherRequest Request)
    : IRequest<UserEagleFeatherResponse>;

public class ApproveUserEagleFeatherHandler : IRequestHandler<ApproveUserEagleFeatherCommand, UserEagleFeatherResponse>
{
    private readonly AppDbContext _db;

    public ApproveUserEagleFeatherHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserEagleFeatherResponse> Handle(ApproveUserEagleFeatherCommand command, CancellationToken cancellationToken)
    {
        var leader = await _db.Users.FindAsync([command.LeaderId], cancellationToken)
            ?? throw new NotFoundException("errors.user_not_found");

        if (leader.Role != UserRole.Leader)
            throw new ForbiddenException("errors.leader_only");

        var uef = await _db.UserEagleFeathers.FindAsync([command.UserEagleFeatherId], cancellationToken)
            ?? throw new NotFoundException("errors.user_eagle_feather_not_found");

        if (uef.Status != EagleFeatherStatus.Pending)
            throw new ConflictException("errors.user_eagle_feather_already_moderated");

        uef.Status = EagleFeatherStatus.Approved;
        uef.VerifiedById = command.LeaderId;
        uef.EarnedAt = DateTime.UtcNow;
        uef.ModeratorNote = command.Request.Note;
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
