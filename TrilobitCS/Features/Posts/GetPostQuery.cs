using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Posts;

public record GetPostQuery(int PostId) : IRequest<PostResponse>;

public class GetPostHandler : IRequestHandler<GetPostQuery, PostResponse>
{
    private readonly AppDbContext _db;

    public GetPostHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PostResponse> Handle(GetPostQuery query, CancellationToken cancellationToken)
    {
        return await _db.Posts
            .Where(p => p.Id == query.PostId)
            .Select(p => new PostResponse(
                p.Id,
                new PostAuthorResponse(p.User.Id, p.User.Nickname, p.User.ProfilePicture),
                p.OrganisationId,
                p.Content,
                p.ImageUrl,
                p.UserEagleFeatherId,
                p.ChallengeId,
                0,
                0,
                p.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("errors.post_not_found");
    }
}
