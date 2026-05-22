using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrilobitCS.Extensions;
using TrilobitCS.Features.Posts;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Controllers;

[ApiController]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PostsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Get a post by ID</summary>
    /// <response code="200">Post detail</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Post not found</response>
    [HttpGet("api/posts/{id:int}")]
    [ProducesResponseType(typeof(PostResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetPostQuery(id), ct));

    /// <summary>Create a new post for a user eagle feather</summary>
    /// <response code="200">Created post</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">UserEagleFeather does not belong to the authenticated user</response>
    /// <response code="404">UserEagleFeatherId or OrganisationId not found</response>
    /// <response code="422">Content and imageUrl both missing</response>
    [HttpPost("api/user-eagle-feathers/{uefId:int}/posts")]
    [ProducesResponseType(typeof(PostResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Create(int uefId, CreatePostRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreatePostCommand(User.GetUserId(), uefId, request), ct));

    /// <summary>Update content or image of own post</summary>
    /// <response code="200">Updated post</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Post does not belong to the authenticated user</response>
    /// <response code="404">Post not found</response>
    /// <response code="422">Content and imageUrl both missing</response>
    [HttpPut("api/posts/{id:int}")]
    [ProducesResponseType(typeof(PostResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Update(int id, UpdatePostRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdatePostCommand(id, User.GetUserId(), request), ct));

    /// <summary>Delete own post</summary>
    /// <response code="204">Post deleted</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Post does not belong to the authenticated user</response>
    /// <response code="404">Post not found</response>
    [HttpDelete("api/posts/{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeletePostCommand(id, User.GetUserId()), ct);
        return NoContent();
    }
}
