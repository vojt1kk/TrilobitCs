using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrilobitCS.Extensions;
using TrilobitCS.Features.Organisations;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Controllers;

[ApiController]
[Authorize]
public class OrganisationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganisationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Create an organisation (Leader only)</summary>
    /// <response code="200">Created organisation</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">User is not a Leader</response>
    /// <response code="422">Leader already has an organisation or invalid data</response>
    [HttpPost("api/organisations")]
    [ProducesResponseType(typeof(OrganisationResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Create(CreateOrganisationRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateOrganisationCommand(User.GetUserId(), request), ct));

    /// <summary>Returns organisation details</summary>
    /// <response code="200">Organisation details</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Organisation not found</response>
    [HttpGet("api/organisations/{id:int}")]
    [ProducesResponseType(typeof(OrganisationResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Show(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetOrganisationQuery(id), ct));

    /// <summary>Update an organisation (leader of that organisation only)</summary>
    /// <response code="200">Updated organisation</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">User is not the leader of this organisation</response>
    /// <response code="404">Organisation not found</response>
    /// <response code="422">Invalid data</response>
    [HttpPut("api/organisations/{id:int}")]
    [ProducesResponseType(typeof(OrganisationResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Update(int id, UpdateOrganisationRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateOrganisationCommand(User.GetUserId(), id, request), ct));

    /// <summary>Returns the list of organisation members</summary>
    /// <response code="200">Member list</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Organisation not found</response>
    [HttpGet("api/organisations/{id:int}/members")]
    [ProducesResponseType(typeof(IEnumerable<OrganisationMemberResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Members(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetOrganisationMembersQuery(id), ct));
}
