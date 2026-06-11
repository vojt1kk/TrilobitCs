using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Features.EagleFeathers;
using TrilobitCS.Pagination;
using TrilobitCS.Responses;

namespace TrilobitCS.Controllers;

[ApiController]
[Authorize]
[Route("api/eagle-feathers")]
public class EagleFeathersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AppDbContext _db;

    public EagleFeathersController(IMediator mediator, AppDbContext db)
    {
        _mediator = mediator;
        _db = db;
    }

    /// <summary>Get all eagle feathers (paginated)</summary>
    /// <response code="200">Returns a paginated list of eagle feathers</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="422">Invalid pagination parameters</response>
    [HttpGet]
    [EndpointName("getEagleFeathers")]
    [ProducesResponseType(typeof(PagedResponse<EagleFeatherResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Index([FromQuery] PaginationQuery pagination, CancellationToken ct)
        => Ok(await _mediator.Send(new GetEagleFeathersQuery(pagination), ct));

    /// <summary>Get a single eagle feather by ID</summary>
    /// <param name="id">Eagle feather ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Returns the eagle feather</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Eagle feather not found</response>
    [HttpGet("{id:int}")]
    [EndpointName("getEagleFeatherById")]
    [ProducesResponseType(typeof(EagleFeatherResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Show(int id, CancellationToken ct)
    {
        var feather = await _db.EagleFeathers.FindAsync([id], ct)
            ?? throw new NotFoundException($"EagleFeather {id} not found");

        return Ok(EagleFeatherResponse.FromModel(feather));
    }
}
