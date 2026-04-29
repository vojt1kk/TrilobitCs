using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    /// <summary>Vrátí profil uživatele podle ID</summary>
    /// <response code="200">Profil uživatele</response>
    /// <response code="401">Nepřihlášený uživatel</response>
    /// <response code="404">Uživatel nenalezen</response>
    [HttpGet("api/users/{id:int}")]
    [ProducesResponseType(typeof(UserResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Show(int id)
        => Ok(await _mediator.Send(new GetUserQuery(id)));

    // PUT /api/user
    /// <summary>Aktualizuje profil přihlášeného uživatele</summary>
    /// <response code="200">Aktualizovaný profil</response>
    /// <response code="401">Nepřihlášený uživatel</response>
    /// <response code="422">Nevalidní data nebo nickname už existuje</response>
    [HttpPut("api/user")]
    [ProducesResponseType(typeof(UserResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Update(UpdateUserRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _mediator.Send(new UpdateUserCommand(userId, request)));
    }

    // DELETE /api/user
    /// <summary>Smaže účet přihlášeného uživatele</summary>
    /// <response code="204">Účet smazán</response>
    /// <response code="401">Nepřihlášený uživatel</response>
    [HttpDelete("api/user")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Destroy()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _mediator.Send(new DeleteUserCommand(userId));
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
    public async Task<IActionResult> LeaveOrganisation()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _mediator.Send(new LeaveOrganisationCommand(userId));
        return NoContent();
    }
}
