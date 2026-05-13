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

    // GET /api/users/{id}
    /// <summary>Vrátí veřejný profil uživatele (bez emailu)</summary>
    /// <response code="200">Veřejný profil uživatele</response>
    /// <response code="401">Nepřihlášený uživatel</response>
    /// <response code="404">Uživatel nenalezen</response>
    [HttpGet("api/users/{id:int}")]
    [ProducesResponseType(typeof(PublicUserResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Show(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetUserQuery(id), ct));

    // GET /api/user/me
    /// <summary>Vrátí vlastní profil přihlášeného uživatele (s emailem, role, organisationId)</summary>
    /// <response code="200">Vlastní profil</response>
    /// <response code="401">Nepřihlášený uživatel</response>
    [HttpGet("api/user/me")]
    [ProducesResponseType(typeof(UserMeResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Me(CancellationToken ct)
        => Ok(await _mediator.Send(new GetCurrentUserQuery(User.GetUserId()), ct));

    // PUT /api/user
    /// <summary>Aktualizuje profil přihlášeného uživatele</summary>
    /// <response code="200">Aktualizovaný profil</response>
    /// <response code="401">Nepřihlášený uživatel</response>
    /// <response code="422">Nevalidní data nebo nickname už existuje</response>
    [HttpPut("api/user")]
    [ProducesResponseType(typeof(UserMeResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Update(UpdateUserRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateUserCommand(User.GetUserId(), request), ct));

    // DELETE /api/user
    /// <summary>Smaže účet přihlášeného uživatele</summary>
    /// <response code="204">Účet smazán</response>
    /// <response code="401">Nepřihlášený uživatel</response>
    [HttpDelete("api/user")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Destroy(CancellationToken ct)
    {
        await _mediator.Send(new DeleteUserCommand(User.GetUserId()), ct);
        return NoContent();
    }

    // DELETE /api/user/organisation
    /// <summary>Odchod z organizace (Leader vlastní org nemůže odejít)</summary>
    /// <response code="204">Odchod úspěšný</response>
    /// <response code="401">Nepřihlášený uživatel</response>
    /// <response code="422">Uživatel není v organizaci nebo je Leader</response>
    [HttpDelete("api/user/organisation")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> LeaveOrganisation(CancellationToken ct)
    {
        await _mediator.Send(new LeaveOrganisationCommand(User.GetUserId()), ct);
        return NoContent();
    }
}
