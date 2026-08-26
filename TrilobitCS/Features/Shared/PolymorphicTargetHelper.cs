using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Models;

namespace TrilobitCS.Features.Shared;

public static class PolymorphicTargetHelper
{
    public static Task<bool> LikeableTargetExistsAsync(AppDbContext db, LikeableType type, int id, CancellationToken ct)
        => type switch
        {
            LikeableType.Posts => db.Posts.AnyAsync(p => p.Id == id, ct),
            LikeableType.Comments => db.Comments.AnyAsync(c => c.Id == id, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public static Task<bool> CommentableTargetExistsAsync(AppDbContext db, CommentableType type, int id, CancellationToken ct)
        => type switch
        {
            CommentableType.Posts => db.Posts.AnyAsync(p => p.Id == id, ct),
            CommentableType.Comments => db.Comments.AnyAsync(c => c.Id == id, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}
