using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrilobitCS.Extensions;
using TrilobitCS.Features.Challenges;
using TrilobitCS.Responses;

namespace TrilobitCS.Controllers;

[ApiController]
[Authorize]
public class ChallengesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChallengesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns the newest active challenge with the caller's pin state</summary>
    /// <response code="200">Current challenge</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">No active challenge</response>
    [HttpGet("api/challenges/current")]
    [EndpointName("getCurrentChallenge")]
    [ProducesResponseType(typeof(CurrentChallengeResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
        => Ok(await _mediator.Send(new GetCurrentChallengeQuery(User.GetUserId()), ct));

    /// <summary>Returns a challenge by id</summary>
    /// <response code="200">Challenge</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Challenge not found</response>
    [HttpGet("api/challenges/{id:int}")]
    [EndpointName("getChallenge")]
    [ProducesResponseType(typeof(ChallengeResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetChallengeQuery(id), ct));

    /// <summary>Pins an active challenge for the caller</summary>
    /// <response code="200">Pinned challenge</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Challenge not found or not active</response>
    /// <response code="422">Already pinned</response>
    [HttpPost("api/challenges/{id:int}/pin")]
    [EndpointName("pinChallenge")]
    [ProducesResponseType(typeof(CurrentChallengeResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Pin(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new PinChallengeCommand(User.GetUserId(), id), ct));

    /// <summary>Unpins a challenge for the caller</summary>
    /// <response code="200">Unpinned challenge</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Challenge not found</response>
    /// <response code="422">Challenge is not pinned</response>
    [HttpPost("api/challenges/{id:int}/unpin")]
    [EndpointName("unpinChallenge")]
    [ProducesResponseType(typeof(CurrentChallengeResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Unpin(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new UnpinChallengeCommand(User.GetUserId(), id), ct));
}
