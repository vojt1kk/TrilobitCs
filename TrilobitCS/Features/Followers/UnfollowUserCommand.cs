using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;

namespace TrilobitCS.Features.Followers;

public record UnfollowUserCommand(int CurrentUserId, int TargetUserId) : IRequest;

public class UnfollowUserHandler : IRequestHandler<UnfollowUserCommand>
{
    private readonly AppDbContext _db;

    public UnfollowUserHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(UnfollowUserCommand command, CancellationToken cancellationToken)
    {
        var follower = await _db.Followers.FirstOrDefaultAsync(
            f => f.FollowerId == command.CurrentUserId && f.FollowingId == command.TargetUserId,
            cancellationToken);
        if (follower is null)
            return;

        _db.Followers.Remove(follower);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Concurrent unfollow already deleted this row (0 rows affected) — desired end state
            // (no follow relation) already holds, so treat as success.
        }
    }
}
