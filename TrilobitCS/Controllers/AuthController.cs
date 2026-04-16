using MediatR;
using Microsoft.AspNetCore.Mvc;
using TrilobitCS.Features.Auth;
using TrilobitCS.Requests;
using TrilobitCS.Responses;

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
    /// <summary>Register a new user</summary>
    /// <response code="200">Returns access token and refresh token</response>
    /// <response code="422">Invalid data (password mismatch, duplicate email/nickname)</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Register(RegisterRequest request)
        => Ok(await _mediator.Send(new RegisterCommand(request)));

    // POST /api/auth/login
    /// <summary>Log in with nickname and password</summary>
    /// <response code="200">Returns access token and refresh token</response>
    /// <response code="401">Invalid credentials</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login(LoginRequest request)
        => Ok(await _mediator.Send(new LoginCommand(request)));

    // POST /api/auth/refresh
    /// <summary>Exchange a refresh token for a new token pair (rotation)</summary>
    /// <response code="200">Returns new access token and refresh token</response>
    /// <response code="401">Invalid or already used refresh token</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Refresh(RefreshRequest request)
        => Ok(await _mediator.Send(new RefreshCommand(request)));

    // POST /api/auth/logout
    /// <summary>Log out — revoke the refresh token</summary>
    /// <response code="204">Token successfully revoked</response>
    /// <response code="404">Token not found</response>
    [HttpPost("logout")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Logout(RefreshRequest request)
    {
        await _mediator.Send(new LogoutCommand(request));
        return NoContent();
    }
}
