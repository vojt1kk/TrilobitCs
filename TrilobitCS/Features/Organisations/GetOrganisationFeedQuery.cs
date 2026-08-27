using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;
using TrilobitCS.Pagination;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Organisations;

public record GetOrganisationFeedQuery(int OrganisationId, PaginationQuery Pagination) : IRequest<PagedResponse<PostResponse>>;

public class GetOrganisationFeedHandler : IRequestHandler<GetOrganisationFeedQuery, PagedResponse<PostResponse>>
{
    private readonly AppDbContext _db;

    public GetOrganisationFeedHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<PostResponse>> Handle(GetOrganisationFeedQuery query, CancellationToken cancellationToken)
    {
        if (!await _db.Organisations.AnyAsync(o => o.Id == query.OrganisationId, cancellationToken))
            throw new NotFoundException("errors.organisation_not_found");

        // Same identity-selector + Id-tiebreaker rationale as GetPersonalFeedQuery.
        return await _db.Posts
            .Where(p => p.OrganisationId == query.OrganisationId)
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
