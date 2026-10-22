using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Features.Shared;
using TrilobitCS.Models;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Posts;

public record GetPostQuery(int PostId, int ViewerId) : IRequest<PostResponse>;

public class GetPostHandler : IRequestHandler<GetPostQuery, PostResponse>
{
    private readonly AppDbContext _db;

    public GetPostHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PostResponse> Handle(GetPostQuery query, CancellationToken cancellationToken)
    {
        var viewerOrgId = await PrivacyGuard.GetViewerOrganisationIdAsync(_db, query.ViewerId, cancellationToken);

        return await _db.Posts
            .Where(p => p.Id == query.PostId)
            .Where(PrivacyGuard.PostVisibleTo(query.ViewerId, viewerOrgId))
            .Select(p => new PostResponse(
                p.Id,
                new PostAuthorResponse(p.User.Id, p.User.Nickname, p.User.ProfilePicture),
                p.OrganisationId,
                p.Content,
                p.ImageUrl,
                p.UserEagleFeatherId,
                p.ChallengeId,
                _db.Likes.Count(l => l.LikeableType == LikeableType.Posts && l.LikeableId == p.Id),
                _db.Comments.Count(c => c.CommentableType == CommentableType.Posts && c.CommentableId == p.Id),
                p.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("errors.post_not_found");
    }
}
