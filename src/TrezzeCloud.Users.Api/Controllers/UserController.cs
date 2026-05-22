using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TrezzeCloud.Users.Application.Abstractions;
using TrezzeCloud.Users.Application.DTOs;
using MassTransit;
using TrezzeCloud.Contracts.Events;

namespace TrezzeCloud.Users.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UserController : ControllerBase
{
    private readonly IIdentityService _identityService;
    private readonly IPublishEndpoint _publishEndpoint;

    public UserController(
    IIdentityService identityService,
    IPublishEndpoint publishEndpoint)
    {
        _identityService = identityService;
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
    RegisterUserRequest request)
    {
        var response = await _identityService.RegisterAsync(request);

        await _publishEndpoint.Publish(new UserCreatedEvent(
            response.UserId,
            response.Name,
            response.Email,
            DateTime.UtcNow));

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginUserRequest request)
    {
        var response = await _identityService.LoginAsync(request);

        return Ok(response);
    }

    [HttpPost("refresh-login")]
    public async Task<IActionResult> RefreshLogin(
        RefreshLoginRequest request)
    {
        var response = await _identityService.RefreshLoginAsync(request);

        return Ok(response);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _identityService.GetUsersAsync();

        return Ok(users);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _identityService.GetUserByIdAsync(id);

        if (user is null)
            return NotFound();

        return Ok(user);
    }
}