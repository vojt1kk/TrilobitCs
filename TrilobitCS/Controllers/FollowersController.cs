using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrilobitCS.Extensions;
using TrilobitCS.Features.Followers;
using TrilobitCS.Pagination;
using TrilobitCS.Responses;

namespace TrilobitCS.Controllers;

[ApiController]
[Authorize]
public class FollowersController : ControllerBase
{
    private readonly IMediator _mediator;

    public FollowersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Follow a user (idempotent — repeat calls return the existing relation)</summary>
    /// <response code="200">Follow relation created or already existed</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Target user not found</response>
    /// <response code="422">Cannot follow yourself</response>
    [HttpPost("api/users/{id:int}/follow")]
    [EndpointName("followUser")]
    [EnableRateLimiting("social")]
    [ProducesResponseType(typeof(FollowResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Follow(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new FollowUserCommand(User.GetUserId(), id), ct));

    /// <summary>Unfollow a user (no-op if not currently following)</summary>
    /// <response code="204">Follow relation removed or already absent</response>
    /// <response code="401">Unauthorized</response>
    [HttpDelete("api/users/{id:int}/follow")]
    [EndpointName("unfollowUser")]
    [EnableRateLimiting("social")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Unfollow(int id, CancellationToken ct)
    {
        await _mediator.Send(new UnfollowUserCommand(User.GetUserId(), id), ct);
        return NoContent();
    }

    /// <summary>Returns the list of users following the given user (paginated)</summary>
    /// <response code="200">Followers list</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">User not found</response>
    /// <response code="422">Invalid pagination parameters</response>
    [HttpGet("api/users/{id:int}/followers")]
    [EndpointName("getFollowers")]
    [ProducesResponseType(typeof(PagedResponse<PublicUserResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Followers(int id, [FromQuery] PaginationQuery pagination, CancellationToken ct)
        => Ok(await _mediator.Send(new GetFollowersQuery(id, pagination), ct));

    /// <summary>Returns the list of users the given user is following (paginated)</summary>
    /// <response code="200">Following list</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">User not found</response>
    /// <response code="422">Invalid pagination parameters</response>
    [HttpGet("api/users/{id:int}/following")]
    [EndpointName("getFollowing")]
    [ProducesResponseType(typeof(PagedResponse<PublicUserResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Following(int id, [FromQuery] PaginationQuery pagination, CancellationToken ct)
        => Ok(await _mediator.Send(new GetFollowingQuery(id, pagination), ct));
}
