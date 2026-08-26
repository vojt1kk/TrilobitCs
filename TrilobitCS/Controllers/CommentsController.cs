using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrilobitCS.Extensions;
using TrilobitCS.Features.Comments;
using TrilobitCS.Models;
using TrilobitCS.Pagination;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Controllers;

[ApiController]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Create a comment on a post</summary>
    /// <response code="200">Created comment</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Post not found</response>
    /// <response code="422">Content missing or too long</response>
    [HttpPost("api/posts/{id:int}/comments")]
    [EndpointName("createPostComment")]
    [EnableRateLimiting("social")]
    [ProducesResponseType(typeof(CommentResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> CreateOnPost(int id, CreateCommentRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateCommentCommand(User.GetUserId(), CommentableType.Posts, id, request), ct));

    /// <summary>Get comments on a post (direct children only, paginated)</summary>
    /// <response code="200">Comments list</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Post not found</response>
    /// <response code="422">Invalid pagination parameters</response>
    [HttpGet("api/posts/{id:int}/comments")]
    [EndpointName("getPostComments")]
    [ProducesResponseType(typeof(PagedResponse<CommentResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> GetForPost(int id, [FromQuery] PaginationQuery pagination, CancellationToken ct)
        => Ok(await _mediator.Send(new GetCommentsQuery(CommentableType.Posts, id, pagination), ct));

    /// <summary>Create a reply to a comment</summary>
    /// <response code="200">Created comment</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Comment not found</response>
    /// <response code="422">Content missing or too long</response>
    [HttpPost("api/comments/{id:int}/comments")]
    [EndpointName("createCommentReply")]
    [EnableRateLimiting("social")]
    [ProducesResponseType(typeof(CommentResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> CreateOnComment(int id, CreateCommentRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateCommentCommand(User.GetUserId(), CommentableType.Comments, id, request), ct));

    /// <summary>Get replies to a comment (direct children only, paginated)</summary>
    /// <response code="200">Comments list</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Comment not found</response>
    /// <response code="422">Invalid pagination parameters</response>
    [HttpGet("api/comments/{id:int}/comments")]
    [EndpointName("getCommentReplies")]
    [ProducesResponseType(typeof(PagedResponse<CommentResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> GetForComment(int id, [FromQuery] PaginationQuery pagination, CancellationToken ct)
        => Ok(await _mediator.Send(new GetCommentsQuery(CommentableType.Comments, id, pagination), ct));

    /// <summary>Update own comment</summary>
    /// <response code="200">Updated comment</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Comment does not belong to the authenticated user</response>
    /// <response code="404">Comment not found</response>
    /// <response code="422">Content missing or too long</response>
    [HttpPut("api/comments/{id:int}")]
    [EndpointName("updateComment")]
    [EnableRateLimiting("social")]
    [ProducesResponseType(typeof(CommentResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Update(int id, UpdateCommentRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateCommentCommand(id, User.GetUserId(), request), ct));

    /// <summary>Delete own comment (recursively cascades all descendant replies and their likes)</summary>
    /// <response code="204">Comment (and descendant replies) deleted</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Comment does not belong to the authenticated user</response>
    /// <response code="404">Comment not found</response>
    [HttpDelete("api/comments/{id:int}")]
    [EndpointName("deleteComment")]
    [EnableRateLimiting("social")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteCommentCommand(id, User.GetUserId()), ct);
        return NoContent();
    }
}
