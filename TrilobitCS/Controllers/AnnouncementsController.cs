using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrilobitCS.Extensions;
using TrilobitCS.Features.Announcements;
using TrilobitCS.Pagination;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

namespace TrilobitCS.Controllers;

[ApiController]
[Authorize]
public class AnnouncementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnnouncementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Create an announcement for an organisation (Leader of that organisation only)</summary>
    /// <response code="201">Created announcement</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">User is not the leader of this organisation</response>
    /// <response code="404">Organisation not found</response>
    /// <response code="422">Invalid data</response>
    [HttpPost("api/organisations/{id:int}/announcements")]
    [EndpointName("createAnnouncement")]
    [ProducesResponseType(typeof(AnnouncementResponse), 201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Create(int id, CreateAnnouncementRequest request, CancellationToken ct)
        => StatusCode(201, await _mediator.Send(new CreateAnnouncementCommand(User.GetUserId(), id, request), ct));

    /// <summary>Returns the announcements of an organisation (paginated, members only)</summary>
    /// <response code="200">Announcement list</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">User is not a member of this organisation</response>
    /// <response code="404">Organisation not found</response>
    /// <response code="422">Invalid pagination parameters</response>
    [HttpGet("api/organisations/{id:int}/announcements")]
    [EndpointName("getAnnouncements")]
    [ProducesResponseType(typeof(PagedResponse<AnnouncementResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Index(int id, [FromQuery] PaginationQuery pagination, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAnnouncementsQuery(id, User.GetUserId(), pagination), ct));

    /// <summary>Update an announcement (Leader of that announcement's organisation only)</summary>
    /// <response code="200">Updated announcement</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">User is not the leader of this announcement's organisation</response>
    /// <response code="404">Announcement not found</response>
    /// <response code="422">Invalid data</response>
    [HttpPut("api/announcements/{id:int}")]
    [EndpointName("updateAnnouncement")]
    [ProducesResponseType(typeof(AnnouncementResponse), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Update(int id, UpdateAnnouncementRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateAnnouncementCommand(id, User.GetUserId(), request), ct));

    /// <summary>Delete an announcement (Leader of that announcement's organisation only)</summary>
    /// <response code="204">Announcement deleted</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">User is not the leader of this announcement's organisation</response>
    /// <response code="404">Announcement not found</response>
    [HttpDelete("api/announcements/{id:int}")]
    [EndpointName("deleteAnnouncement")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteAnnouncementCommand(id, User.GetUserId()), ct);
        return NoContent();
    }
}
