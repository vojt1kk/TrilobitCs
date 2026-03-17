using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrilobitCS.Data;
using TrilobitCS.Exceptions;
using TrilobitCS.Responses;

namespace TrilobitCS.Controllers;

[ApiController]
[Route("api/eagle-feathers")]
public class EagleFeathersController : ControllerBase
{
    private readonly AppDbContext _db;

    public EagleFeathersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var feathers = await _db.EagleFeathers.ToListAsync();
        return Ok(feathers.Select(EagleFeatherResponse.FromModel));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Show(int id)
    {
        var feather = await _db.EagleFeathers.FindAsync(id)
            ?? throw new NotFoundException($"EagleFeather {id} not found");

        return Ok(EagleFeatherResponse.FromModel(feather));
    }
}
