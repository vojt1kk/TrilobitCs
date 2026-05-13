using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Responses;

namespace TrilobitCS.Controllers;

[ApiController]
[Authorize]
[Route("api/eagle-feathers")]
public class EagleFeathersController : ControllerBase
{
    private readonly AppDbContext _db;

    public EagleFeathersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Get all eagle feathers</summary>
    /// <response code="200">Returns an array of eagle feathers</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EagleFeatherResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var feathers = await _db.EagleFeathers.ToListAsync(ct);
        return Ok(feathers.Select(EagleFeatherResponse.FromModel));
    }

    /// <summary>Get a single eagle feather by ID</summary>
    /// <param name="id">Eagle feather ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Returns the eagle feather</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Eagle feather not found</response>
    [HttpGet("{id:int}")]
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
