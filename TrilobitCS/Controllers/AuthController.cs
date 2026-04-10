using MediatR;
using Microsoft.AspNetCore.Mvc;
using TrilobitCS.Features.Auth;
using TrilobitCS.Requests;

namespace TrilobitCS.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
        => Ok(await _mediator.Send(new RegisterCommand(request)));

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
        => Ok(await _mediator.Send(new LoginCommand(request)));

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request)
        => Ok(await _mediator.Send(new RefreshCommand(request)));

    // POST /api/auth/logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request)
    {
        await _mediator.Send(new LogoutCommand(request));
        return NoContent();
    }
}
