using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrilobitCS.Features.OrganisationInvites;
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

    // POST /api/organisation-invites
    /// <summary>Pošle pozvánku uživateli podle nickname (pouze Leader)</summary>
    /// <response code="200">Vytvořená pozvánka</response>
    /// <response code="401">Nepřihlášený uživatel</response>
    /// <response code="403">Uživatel není Leader s organizací</response>
    /// <response code="404">Cílový uživatel nenalezen</response>
    /// <response code="422">Uživatel již v organizaci nebo pending pozvánka existuje</response>
    [HttpPost("api/organisation-invites")]
    [ProducesResponseType(typeof(OrganisationInviteResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Send(SendOrganisationInviteRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _mediator.Send(new SendOrganisationInviteCommand(userId, request)));
    }

    // GET /api/organisation-invites
    /// <summary>Vrátí pozvánky přihlášeného uživatele (všechny statusy)</summary>
    /// <response code="200">Seznam pozvánek</response>
    /// <response code="401">Nepřihlášený uživatel</response>
    [HttpGet("api/organisation-invites")]
    [ProducesResponseType(typeof(IEnumerable<OrganisationInviteResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _mediator.Send(new GetOrganisationInvitesQuery(userId)));
    }

    // POST /api/organisation-invites/{id}/accept
    /// <summary>Přijme pozvánku (pozvaný uživatel)</summary>
    /// <response code="200">Přijatá pozvánka</response>
    /// <response code="401">Nepřihlášený uživatel</response>
    /// <response code="404">Pozvánka nenalezena nebo nepatří tomuto uživateli</response>
    /// <response code="422">Pozvánka není Pending nebo uživatel již v organizaci</response>
    [HttpPost("api/organisation-invites/{id:int}/accept")]
    [ProducesResponseType(typeof(OrganisationInviteResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Accept(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _mediator.Send(new AcceptOrganisationInviteCommand(userId, id)));
    }

    // POST /api/organisation-invites/{id}/decline
    /// <summary>Odmítne pozvánku (pozvaný uživatel)</summary>
    /// <response code="200">Odmítnutá pozvánka</response>
    /// <response code="401">Nepřihlášený uživatel</response>
    /// <response code="404">Pozvánka nenalezena nebo nepatří tomuto uživateli</response>
    /// <response code="422">Pozvánka není Pending</response>
    [HttpPost("api/organisation-invites/{id:int}/decline")]
    [ProducesResponseType(typeof(OrganisationInviteResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Decline(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _mediator.Send(new DeclineOrganisationInviteCommand(userId, id)));
    }
}
