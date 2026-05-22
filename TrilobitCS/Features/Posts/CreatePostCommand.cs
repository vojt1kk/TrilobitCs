using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Models;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Posts;

public record CreatePostCommand(int UserId, int UserEagleFeatherId, CreatePostRequest Request) : IRequest<PostResponse>;

public class CreatePostHandler : IRequestHandler<CreatePostCommand, PostResponse>
{
    private readonly AppDbContext _db;

    public CreatePostHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PostResponse> Handle(CreatePostCommand command, CancellationToken cancellationToken)
    {
        var uef = await _db.UserEagleFeathers.FindAsync([command.UserEagleFeatherId], cancellationToken)
            ?? throw new NotFoundException("errors.user_eagle_feather_not_found");
        if (uef.UserId != command.UserId)
            throw new ForbiddenException("errors.forbidden");

        var req = command.Request;

        if (req.OrganisationId is not null
            && !await _db.Organisations.AnyAsync(o => o.Id == req.OrganisationId.Value, cancellationToken))
            throw new NotFoundException("errors.organisation_not_found");

        var post = new Post
        {
            UserId = command.UserId,
            OrganisationId = req.OrganisationId,
            UserEagleFeatherId = command.UserEagleFeatherId,
            ChallengeId = req.ChallengeId,
            Content = req.Content,
            ImageUrl = req.ImageUrl,
            CreatedAt = DateTime.UtcNow,
        };

        uef.IsCompleted = true;
        _db.Posts.Add(post);
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
