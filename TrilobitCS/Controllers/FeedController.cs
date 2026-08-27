using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrilobitCS.Extensions;
using TrilobitCS.Features.Feed;
using TrilobitCS.Pagination;
using TrilobitCS.Responses;

namespace TrilobitCS.Controllers;

[ApiController]
[Authorize]
public class FeedController : ControllerBase
{
    private readonly IMediator _mediator;

    public FeedController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns the current user's personal feed: own posts + posts of followed users (paginated)</summary>
    /// <response code="200">Feed page</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="422">Invalid pagination parameters</response>
    [HttpGet("api/feed")]
    [EndpointName("getFeed")]
    [ProducesResponseType(typeof(PagedResponse<PostResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Index([FromQuery] PaginationQuery pagination, CancellationToken ct)
        => Ok(await _mediator.Send(new GetPersonalFeedQuery(User.GetUserId(), pagination), ct));
}
