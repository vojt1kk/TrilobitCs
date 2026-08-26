using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Features.Shared;
using TrilobitCS.Models;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Likes;

public record LikeTargetCommand(int UserId, LikeableType Type, int TargetId) : IRequest<LikeResponse>;

public class LikeTargetHandler : IRequestHandler<LikeTargetCommand, LikeResponse>
{
    private readonly AppDbContext _db;

    public LikeTargetHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<LikeResponse> Handle(LikeTargetCommand command, CancellationToken cancellationToken)
    {
        if (!await PolymorphicTargetHelper.LikeableTargetExistsAsync(_db, command.Type, command.TargetId, cancellationToken))
            throw new NotFoundException("errors.likeable_target_not_found");

        var existing = await _db.Likes.FirstOrDefaultAsync(
            l => l.UserId == command.UserId && l.LikeableType == command.Type && l.LikeableId == command.TargetId,
            cancellationToken);
        if (existing is not null)
            return new LikeResponse(existing.Id, existing.UserId, existing.LikeableType, existing.LikeableId, existing.CreatedAt);

        var like = new Like
        {
            UserId = command.UserId,
            LikeableType = command.Type,
            LikeableId = command.TargetId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Likes.Add(like);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // Concurrent double-tap: another request already inserted the same row. Re-fetch and
            // return it instead of surfacing the unique-violation as a 500 (idempotent semantics).
            var raced = await _db.Likes.FirstAsync(
                l => l.UserId == command.UserId && l.LikeableType == command.Type && l.LikeableId == command.TargetId,
                cancellationToken);
            return new LikeResponse(raced.Id, raced.UserId, raced.LikeableType, raced.LikeableId, raced.CreatedAt);
        }

        return new LikeResponse(like.Id, like.UserId, like.LikeableType, like.LikeableId, like.CreatedAt);
    }
}
