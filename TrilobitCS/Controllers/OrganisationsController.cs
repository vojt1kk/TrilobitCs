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

    // POST /api/organisations
    /// <summary>Vytvoří organizaci (pouze Leader)</summary>
    /// <response code="200">Vytvořená organizace</response>
    /// <response code="401">Nepřihlášený uživatel</response>
    /// <response code="403">Uživatel není Leader</response>
    /// <response code="422">Leader již má organizaci nebo nevalidní data</response>
    [HttpPost("api/organisations")]
    [ProducesResponseType(typeof(OrganisationResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Create(CreateOrganisationRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateOrganisationCommand(User.GetUserId(), request), ct));

    // GET /api/organisations/{id}
    /// <summary>Vrátí detail organizace</summary>
    /// <response code="200">Detail organizace</response>
    /// <response code="401">Nepřihlášený uživatel</response>
    /// <response code="404">Organizace nenalezena</response>
    [HttpGet("api/organisations/{id:int}")]
    [ProducesResponseType(typeof(OrganisationResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Show(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetOrganisationQuery(id), ct));

    // PUT /api/organisations/{id}
    /// <summary>Aktualizuje organizaci (pouze Leader dané org)</summary>
    /// <response code="200">Aktualizovaná organizace</response>
    /// <response code="401">Nepřihlášený uživatel</response>
    /// <response code="403">Uživatel není Leader této organizace</response>
    /// <response code="404">Organizace nenalezena</response>
    /// <response code="422">Nevalidní data</response>
    [HttpPut("api/organisations/{id:int}")]
    [ProducesResponseType(typeof(OrganisationResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Update(int id, UpdateOrganisationRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateOrganisationCommand(User.GetUserId(), id, request), ct));

    // GET /api/organisations/{id}/members
    /// <summary>Vrátí seznam členů organizace</summary>
    /// <response code="200">Seznam členů</response>
    /// <response code="401">Nepřihlášený uživatel</response>
    /// <response code="404">Organizace nenalezena</response>
    [HttpGet("api/organisations/{id:int}/members")]
    [ProducesResponseType(typeof(IEnumerable<OrganisationMemberResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Members(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetOrganisationMembersQuery(id), ct));
}
