using MediatR;
using TrilobitCS.Data;
using TrilobitCS.Models;
using TrilobitCS.Pagination;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Feed;

public record GetPersonalFeedQuery(int UserId, PaginationQuery Pagination) : IRequest<PagedResponse<PostResponse>>;

public class GetPersonalFeedHandler : IRequestHandler<GetPersonalFeedQuery, PagedResponse<PostResponse>>
{
    private readonly AppDbContext _db;

    public GetPersonalFeedHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<PostResponse>> Handle(GetPersonalFeedQuery query, CancellationToken cancellationToken)
    {
        var followedIds = _db.Followers
            .Where(f => f.FollowerId == query.UserId)
            .Select(f => f.FollowingId);

        // Identity selector: LikeCount/CommentCount subqueries below must be evaluated server-side
        // (like GetPostQuery.cs), so ToPagedResponseAsync's in-memory selector is just a pass-through.
        // CreatedAt DESC needs an Id tiebreaker for a stable total order under offset pagination.
        return await _db.Posts
            .Where(p => p.UserId == query.UserId || followedIds.Contains(p.UserId))
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
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
            .ToPagedResponseAsync(query.Pagination, x => x, cancellationToken);
    }
}
