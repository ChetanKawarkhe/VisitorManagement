using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagement.Application.DTOs.Visitors;
using VisitorManagement.Application.Exceptions;
using VisitorManagement.Application.Interfaces;

namespace VisitorManagement.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,User")]
[Route("api/visitors")]
public sealed class VisitorsController : ControllerBase
{
    private readonly IVisitorService _visitorService;
    private readonly ILogger<VisitorsController> _logger;

    public VisitorsController(
        IVisitorService visitorService,
        ILogger<VisitorsController> logger)
    {
        _visitorService = visitorService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VisitorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateVisitorRequest request,
        CancellationToken cancellationToken)
    {
        int currentUserId = GetCurrentUserId();

        try
        {
            var visitor = await _visitorService.CreateAsync(
                request,
                currentUserId,
                cancellationToken);

            _logger.LogInformation(
                "Visitor created successfully. VisitorId: {VisitorId}",
                visitor.Id);

            return CreatedAtAction(
                nameof(GetById),
                new { id = visitor.Id },
                visitor);
        }
        catch (ValidationException ex)
        {
            return CreateValidationResponse(ex);
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<VisitorResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] VisitorSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _visitorService.SearchAsync(
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(VisitorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var visitor = await _visitorService.GetByIdAsync(
            id,
            cancellationToken);

        if (visitor is null)
        {
            return NotFound(new
            {
                Message = "Visitor record was not found."
            });
        }

        return Ok(visitor);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VisitorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateVisitorRequest request,
        CancellationToken cancellationToken)
    {
        int currentUserId = GetCurrentUserId();

        try
        {
            var visitor = await _visitorService.UpdateAsync(
                id,
                request,
                currentUserId,
                cancellationToken);

            _logger.LogInformation(
                "Visitor updated successfully. VisitorId: {VisitorId}",
                visitor.Id);

            return Ok(visitor);
        }
        catch (ValidationException ex)
        {
            return CreateValidationResponse(ex);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new
            {
                Message = ex.Message
            });
        }
        catch (ConcurrencyConflictException ex)
        {
            return Conflict(new
            {
                Message = ex.Message
            });
        }
    }

    private int GetCurrentUserId()
    {
        string? userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userId, out int id))
        {
            throw new UnauthorizedAccessException(
                "Authenticated user identifier is invalid.");
        }

        return id;
    }

    private BadRequestObjectResult CreateValidationResponse(
        ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(x => x.ErrorMessage)
                    .ToArray());

        return BadRequest(new
        {
            Status = 400,
            Title = "Validation Failed",
            Message = "Please correct the highlighted fields.",
            Errors = errors
        });
    }
}