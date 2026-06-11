using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrilobitCS.Extensions;
using TrilobitCS.Features.Users;
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

    /// <summary>Returns the public profile of a user (without email)</summary>
    /// <response code="200">Public user profile</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">User not found</response>
    [HttpGet("api/users/{id:int}")]
    [EndpointName("getUser")]
    [ProducesResponseType(typeof(PublicUserResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Show(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetUserQuery(id), ct));

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
