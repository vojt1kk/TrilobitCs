using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Posts;

public record UpdatePostCommand(int PostId, int UserId, UpdatePostRequest Request) : IRequest<PostResponse>;

public class UpdatePostHandler : IRequestHandler<UpdatePostCommand, PostResponse>
{
    private readonly AppDbContext _db;

    public UpdatePostHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PostResponse> Handle(UpdatePostCommand command, CancellationToken cancellationToken)
    {
        var post = await _db.Posts.FindAsync([command.PostId], cancellationToken)
            ?? throw new NotFoundException("errors.post_not_found");

        if (post.UserId != command.UserId)
            throw new ForbiddenException("errors.forbidden");

        post.Content = command.Request.Content;
        post.ImageUrl = command.Request.ImageUrl;

        await _db.SaveChangesAsync(cancellationToken);

        return await _db.Posts
            .Where(p => p.Id == post.Id)
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
            .FirstAsync(cancellationToken);
    }
}
