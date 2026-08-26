using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Followers;

public record FollowUserCommand(int CurrentUserId, int TargetUserId) : IRequest<FollowResponse>;

public class FollowUserHandler : IRequestHandler<FollowUserCommand, FollowResponse>
{
    private readonly AppDbContext _db;

    public FollowUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<FollowResponse> Handle(FollowUserCommand command, CancellationToken cancellationToken)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == command.TargetUserId, cancellationToken))
            throw new NotFoundException("errors.user_not_found");

        // Explicit pre-check avoids the raw DB CHECK(follower_id <> following_id) constraint
        // surfacing as an unhandled 500.
        if (command.TargetUserId == command.CurrentUserId)
            throw new ConflictException("errors.cannot_follow_self");

        var existing = await _db.Followers.FirstOrDefaultAsync(
            f => f.FollowerId == command.CurrentUserId && f.FollowingId == command.TargetUserId,
            cancellationToken);
        if (existing is not null)
            return new FollowResponse(existing.FollowerId, existing.FollowingId, existing.CreatedAt);

        var follower = new Follower
        {
            FollowerId = command.CurrentUserId,
            FollowingId = command.TargetUserId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Followers.Add(follower);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // Concurrent double-tap: another request already inserted the same row. Re-fetch and
            // return it instead of surfacing the unique-violation as a 500 (idempotent semantics).
            var raced = await _db.Followers.FirstAsync(
                f => f.FollowerId == command.CurrentUserId && f.FollowingId == command.TargetUserId,
                cancellationToken);
            return new FollowResponse(raced.FollowerId, raced.FollowingId, raced.CreatedAt);
        }

        return new FollowResponse(follower.FollowerId, follower.FollowingId, follower.CreatedAt);
    }
}
