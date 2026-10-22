using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrilobitCS.Extensions;
using TrilobitCS.Features.Users;
using TrilobitCS.Pagination;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Controllers;

[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns the profile of a user (without email); counts are null for private profiles the caller does not follow</summary>
    /// <response code="200">Public user profile</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">User not found</response>
    [HttpGet("api/users/{id:int}")]
    [EndpointName("getUser")]
    [ProducesResponseType(typeof(UserProfileResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Show(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetUserQuery(id, User.GetUserId()), ct));

    /// <summary>Returns a user's posts (paginated; private profiles only to followers)</summary>
    /// <response code="200">Posts page</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Profile is private and the caller does not follow the user</response>
    /// <response code="404">User not found</response>
    /// <response code="422">Invalid pagination parameters</response>
    [HttpGet("api/users/{id:int}/posts")]
    [EndpointName("getUserPosts")]
    [ProducesResponseType(typeof(PagedResponse<PostResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Posts(int id, [FromQuery] PaginationQuery pagination, CancellationToken ct)
        => Ok(await _mediator.Send(new GetUserPostsQuery(id, User.GetUserId(), pagination), ct));

    /// <summary>Returns a user's eagle feathers (paginated; all statuses for self, Approved only for others)</summary>
    /// <response code="200">User eagle feathers page</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Profile is private and the caller does not follow the user</response>
    /// <response code="404">User not found</response>
    /// <response code="422">Invalid pagination parameters</response>
    [HttpGet("api/users/{id:int}/eagle-feathers")]
    [EndpointName("getUserEagleFeathers")]
    [ProducesResponseType(typeof(PagedResponse<UserEagleFeatherResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> EagleFeathers(int id, [FromQuery] PaginationQuery pagination, CancellationToken ct)
        => Ok(await _mediator.Send(new GetUserEagleFeathersQuery(id, User.GetUserId(), pagination), ct));

    /// <summary>Returns aggregate counters of a user (private profiles only to followers)</summary>
    /// <response code="200">User stats</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Profile is private and the caller does not follow the user</response>
    /// <response code="404">User not found</response>
    [HttpGet("api/users/{id:int}/stats")]
    [EndpointName("getUserStats")]
    [ProducesResponseType(typeof(UserStatsResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Stats(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetUserStatsQuery(id, User.GetUserId()), ct));

    /// <summary>Returns the authenticated user's own profile (with email, role, organisationId)</summary>
    /// <response code="200">Own profile</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet("api/user/me")]
    [EndpointName("getCurrentUser")]
    [ProducesResponseType(typeof(UserMeResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Me(CancellationToken ct)
        => Ok(await _mediator.Send(new GetCurrentUserQuery(User.GetUserId()), ct));

    /// <summary>Updates the authenticated user's profile</summary>
    /// <response code="200">Updated profile</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="422">Invalid data or nickname already taken</response>
    [HttpPut("api/user")]
    [EndpointName("updateCurrentUser")]
    [ProducesResponseType(typeof(UserMeResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Update(UpdateUserRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateUserCommand(User.GetUserId(), request), ct));

    /// <summary>Deletes the authenticated user's account</summary>
    /// <response code="204">Account deleted</response>
    /// <response code="401">Unauthorized</response>
    [HttpDelete("api/user")]
    [EndpointName("deleteCurrentUser")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Destroy(CancellationToken ct)
    {
        await _mediator.Send(new DeleteUserCommand(User.GetUserId()), ct);
        return NoContent();
    }

    /// <summary>Leave the current organisation (blocked for the organisation's leader)</summary>
    /// <response code="204">Left successfully</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="422">User is not in an organisation or is the leader</response>
    [HttpDelete("api/user/organisation")]
    [EndpointName("leaveOrganisation")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> LeaveOrganisation(CancellationToken ct)
    {
        await _mediator.Send(new LeaveOrganisationCommand(User.GetUserId()), ct);
        return NoContent();
    }
}
