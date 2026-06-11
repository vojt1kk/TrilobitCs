using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrilobitCS.Extensions;
using TrilobitCS.Features.OrganisationInvites;
using TrilobitCS.Pagination;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Controllers;

[ApiController]
[Authorize]
public class OrganisationInvitesController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganisationInvitesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Send an invite to a user by nickname (Leader only)</summary>
    /// <response code="201">Created invite</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">User is not a Leader with an organisation</response>
    /// <response code="404">Target user not found</response>
    /// <response code="422">User already in an organisation or a pending invite already exists</response>
    [HttpPost("api/organisation-invites")]
    [EndpointName("sendOrganisationInvite")]
    [ProducesResponseType(typeof(OrganisationInviteResponse), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Send(SendOrganisationInviteRequest request, CancellationToken ct)
        => StatusCode(201, await _mediator.Send(new SendOrganisationInviteCommand(User.GetUserId(), request), ct));

    /// <summary>Returns all invites for the authenticated user (all statuses, paginated)</summary>
    /// <response code="200">List of invites</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="422">Invalid pagination parameters</response>
    [HttpGet("api/organisation-invites")]
    [EndpointName("getOrganisationInvites")]
    [ProducesResponseType(typeof(PagedResponse<OrganisationInviteResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Index([FromQuery] PaginationQuery pagination, CancellationToken ct)
        => Ok(await _mediator.Send(new GetOrganisationInvitesQuery(User.GetUserId(), pagination), ct));

    /// <summary>Accept an invite (invited user only)</summary>
    /// <response code="201">Accepted invite</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Invite not found or does not belong to this user</response>
    /// <response code="422">Invite is not pending or user is already in an organisation</response>
    [HttpPost("api/organisation-invites/{id:int}/accept")]
    [EndpointName("acceptOrganisationInvite")]
    [ProducesResponseType(typeof(OrganisationInviteResponse), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Accept(int id, CancellationToken ct)
        => StatusCode(201, await _mediator.Send(new AcceptOrganisationInviteCommand(User.GetUserId(), id), ct));

    /// <summary>Decline an invite (invited user only)</summary>
    /// <response code="201">Declined invite</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Invite not found or does not belong to this user</response>
    /// <response code="422">Invite is not pending</response>
    [HttpPost("api/organisation-invites/{id:int}/decline")]
    [EndpointName("declineOrganisationInvite")]
    [ProducesResponseType(typeof(OrganisationInviteResponse), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Decline(int id, CancellationToken ct)
        => StatusCode(201, await _mediator.Send(new DeclineOrganisationInviteCommand(User.GetUserId(), id), ct));
}
