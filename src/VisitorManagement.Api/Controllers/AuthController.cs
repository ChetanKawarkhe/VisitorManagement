using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagement.Api.Middleware;
using VisitorManagement.Api.Models;
using VisitorManagement.Application.DTOs.Auth;
using VisitorManagement.Application.Interfaces;

namespace VisitorManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(
        IAuthService authService,
        IValidator<LoginRequest> loginValidator)
    {
        _authService = authService;
        _loginValidator = loginValidator;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        await _loginValidator.ValidateAndThrowAsync(
            request,
            cancellationToken);

        var response = await _authService.LoginAsync(
            request,
            cancellationToken);

        if (response is null)
        {
            return Unauthorized(
                CreateErrorResponse(
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized",
                    "Invalid username or password. Please verify your credentials and try again."));
        }

        return Ok(response);
    }

    [HttpGet("profile")]
    [Authorize]
    public IActionResult GetProfile()
    {
        return Ok(new
        {
            Username = User.Identity?.Name,

            UserId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,

            Role = User.FindFirst(
                System.Security.Claims.ClaimTypes.Role)?.Value
        });
    }

    [HttpGet("admin-check")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminCheck()
    {
        return Ok(new
        {
            Message = "Admin authorization successful."
        });
    }

    private ApiErrorResponse CreateErrorResponse(
        int status,
        string title,
        string message)
    {
        string correlationId =
            HttpContext.Items.TryGetValue(
                CorrelationIdMiddleware.ItemKey,
                out var value)
            && value is string id
                ? id
                : Guid.NewGuid().ToString("D");

        return ApiErrorResponse.Create(
            status,
            title,
            message,
            correlationId);
    }
}