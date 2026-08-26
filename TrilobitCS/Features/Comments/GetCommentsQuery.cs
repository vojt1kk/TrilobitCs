using MediatR;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Features.Shared;
using TrilobitCS.Models;
using TrilobitCS.Pagination;
using TrilobitCS.Responses;

namespace TrilobitCS.Features.Comments;

public record GetCommentsQuery(CommentableType Type, int TargetId, PaginationQuery Pagination) : IRequest<PagedResponse<CommentResponse>>;

public class GetCommentsHandler : IRequestHandler<GetCommentsQuery, PagedResponse<CommentResponse>>
{
    private readonly AppDbContext _db;

    public GetCommentsHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<CommentResponse>> Handle(GetCommentsQuery query, CancellationToken cancellationToken)
    {
        if (!await PolymorphicTargetHelper.CommentableTargetExistsAsync(_db, query.Type, query.TargetId, cancellationToken))
            throw new NotFoundException("errors.commentable_target_not_found");

        // Selector below runs in-memory after ToListAsync (ToPagedResponseAsync applies it post-materialization),
        // so the User navigation must be eagerly loaded here or c.User would be null → NullReferenceException.
        return await _db.Comments
            .Where(c => c.CommentableType == query.Type && c.CommentableId == query.TargetId)
            .Include(c => c.User)
            .OrderBy(c => c.Id)
            .ToPagedResponseAsync(
                query.Pagination,
                c => new CommentResponse(
                    c.Id,
                    new PostAuthorResponse(c.User.Id, c.User.Nickname, c.User.ProfilePicture),
                    c.CommentableType,
                    c.CommentableId,
                    c.Content,
                    c.CreatedAt),
                cancellationToken);
    }
}
