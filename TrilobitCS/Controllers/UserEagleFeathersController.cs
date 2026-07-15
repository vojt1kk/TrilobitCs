using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrilobitCS.Extensions;
using TrilobitCS.Features.UserEagleFeathers;
using TrilobitCS.Pagination;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Controllers;

[ApiController]
[Authorize]
public class UserEagleFeathersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserEagleFeathersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Start a new eagle feather attempt (creates a Pending user eagle feather)</summary>
    /// <response code="201">Created user eagle feather</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Eagle feather not found</response>
    /// <response code="422">User already has an attempt for this eagle feather</response>
    [HttpPost("api/user-eagle-feathers")]
    [EndpointName("createUserEagleFeather")]
    [ProducesResponseType(typeof(UserEagleFeatherResponse), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Create(CreateUserEagleFeatherRequest request, CancellationToken ct)
        => StatusCode(201, await _mediator.Send(new CreateUserEagleFeatherCommand(User.GetUserId(), request), ct));

    /// <summary>Returns the authenticated user's eagle feather attempts (paginated)</summary>
    /// <response code="200">List of the user's eagle feathers</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="422">Invalid pagination parameters</response>
    [HttpGet("api/user/eagle-feathers")]
    [EndpointName("getMyUserEagleFeathers")]
    [ProducesResponseType(typeof(PagedResponse<UserEagleFeatherResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Mine([FromQuery] PaginationQuery pagination, CancellationToken ct)
        => Ok(await _mediator.Send(new GetMyUserEagleFeathersQuery(User.GetUserId(), pagination), ct));

    /// <summary>Delete an own eagle feather attempt (cascades attached posts, allowed in any status)</summary>
    /// <response code="204">Deleted</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Attempt does not belong to the authenticated user</response>
    /// <response code="404">Attempt not found</response>
    [HttpDelete("api/user-eagle-feathers/{id:int}")]
    [EndpointName("deleteUserEagleFeather")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteUserEagleFeatherCommand(User.GetUserId(), id), ct);
        return NoContent();
    }

    /// <summary>Retry a rejected attempt (sets status back to Pending)</summary>
    /// <response code="201">Retried attempt</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Attempt does not belong to the authenticated user</response>
    /// <response code="404">Attempt not found</response>
    /// <response code="422">Attempt is not in a Rejected state</response>
    [HttpPost("api/user-eagle-feathers/{id:int}/retry")]
    [EndpointName("retryUserEagleFeather")]
    [ProducesResponseType(typeof(UserEagleFeatherResponse), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Retry(int id, CancellationToken ct)
        => StatusCode(201, await _mediator.Send(new RetryUserEagleFeatherCommand(User.GetUserId(), id), ct));

    /// <summary>Returns all pending eagle feather attempts awaiting moderation (Leader only)</summary>
    /// <response code="200">List of pending attempts</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">User is not a Leader</response>
    /// <response code="422">Invalid pagination parameters</response>
    [HttpGet("api/user-eagle-feathers/pending")]
    [EndpointName("getPendingUserEagleFeathers")]
    [ProducesResponseType(typeof(PagedResponse<UserEagleFeatherResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Pending([FromQuery] PaginationQuery pagination, CancellationToken ct)
        => Ok(await _mediator.Send(new GetPendingUserEagleFeathersQuery(User.GetUserId(), pagination), ct));

    /// <summary>Approve a pending attempt (Leader only)</summary>
    /// <response code="201">Approved attempt</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">User is not a Leader</response>
    /// <response code="404">Attempt not found</response>
    /// <response code="422">Attempt is not pending</response>
    [HttpPost("api/user-eagle-feathers/{id:int}/approve")]
    [EndpointName("approveUserEagleFeather")]
    [ProducesResponseType(typeof(UserEagleFeatherResponse), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Approve(int id, ModerateUserEagleFeatherRequest request, CancellationToken ct)
        => StatusCode(201, await _mediator.Send(new ApproveUserEagleFeatherCommand(User.GetUserId(), id, request), ct));

    /// <summary>Reject a pending attempt (Leader only; attached posts are not deleted)</summary>
    /// <response code="201">Rejected attempt</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">User is not a Leader</response>
    /// <response code="404">Attempt not found</response>
    /// <response code="422">Attempt is not pending</response>
    [HttpPost("api/user-eagle-feathers/{id:int}/reject")]
    [EndpointName("rejectUserEagleFeather")]
    [ProducesResponseType(typeof(UserEagleFeatherResponse), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Reject(int id, ModerateUserEagleFeatherRequest request, CancellationToken ct)
        => StatusCode(201, await _mediator.Send(new RejectUserEagleFeatherCommand(User.GetUserId(), id, request), ct));
}
