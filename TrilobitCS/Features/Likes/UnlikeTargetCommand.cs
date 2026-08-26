using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Models;

namespace TrilobitCS.Features.Likes;

public record UnlikeTargetCommand(int UserId, LikeableType Type, int TargetId) : IRequest;

public class UnlikeTargetHandler : IRequestHandler<UnlikeTargetCommand>
{
    private readonly AppDbContext _db;

    public UnlikeTargetHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(UnlikeTargetCommand command, CancellationToken cancellationToken)
    {
        var like = await _db.Likes.FirstOrDefaultAsync(
            l => l.UserId == command.UserId && l.LikeableType == command.Type && l.LikeableId == command.TargetId,
            cancellationToken);
        if (like is null)
            return;

        _db.Likes.Remove(like);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Concurrent unlike already deleted this row (0 rows affected) — desired end state
            // (no like) already holds, so treat as success.
        }
    }
}
