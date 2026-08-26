using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrilobitCS.Extensions;
using TrilobitCS.Features.Likes;
using TrilobitCS.Models;
using TrilobitCS.Responses;

namespace TrilobitCS.Controllers;

[ApiController]
[Authorize]
public class LikesController : ControllerBase
{
    private readonly IMediator _mediator;

    public LikesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Like a post (idempotent — repeat calls return the existing like; self-like allowed)</summary>
    /// <response code="200">Like created or already existed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Post not found</response>
    [HttpPost("api/posts/{id:int}/likes")]
    [EndpointName("likePost")]
    [EnableRateLimiting("social")]
    [ProducesResponseType(typeof(LikeResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> LikePost(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new LikeTargetCommand(User.GetUserId(), LikeableType.Posts, id), ct));

    /// <summary>Unlike a post (no-op if not currently liked)</summary>
    /// <response code="204">Like removed or already absent</response>
    /// <response code="401">Unauthorized</response>
    [HttpDelete("api/posts/{id:int}/likes")]
    [EndpointName("unlikePost")]
    [EnableRateLimiting("social")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> UnlikePost(int id, CancellationToken ct)
    {
        await _mediator.Send(new UnlikeTargetCommand(User.GetUserId(), LikeableType.Posts, id), ct);
        return NoContent();
    }

    /// <summary>Like a comment (idempotent — repeat calls return the existing like; self-like allowed)</summary>
    /// <response code="200">Like created or already existed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Comment not found</response>
    [HttpPost("api/comments/{id:int}/likes")]
    [EndpointName("likeComment")]
    [EnableRateLimiting("social")]
    [ProducesResponseType(typeof(LikeResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> LikeComment(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new LikeTargetCommand(User.GetUserId(), LikeableType.Comments, id), ct));

    /// <summary>Unlike a comment (no-op if not currently liked)</summary>
    /// <response code="204">Like removed or already absent</response>
    /// <response code="401">Unauthorized</response>
    [HttpDelete("api/comments/{id:int}/likes")]
    [EndpointName("unlikeComment")]
    [EnableRateLimiting("social")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> UnlikeComment(int id, CancellationToken ct)
    {
        await _mediator.Send(new UnlikeTargetCommand(User.GetUserId(), LikeableType.Comments, id), ct);
        return NoContent();
    }
}
