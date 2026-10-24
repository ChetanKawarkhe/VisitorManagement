using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagement.Application.DTOs.Lookups;
using VisitorManagement.Application.Interfaces;

namespace VisitorManagement.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,User")]
[Route("api/purposes")]
public sealed class PurposesController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public PurposesController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<PurposeResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var purposes = await _lookupService.GetPurposesAsync(
            cancellationToken);

        return Ok(purposes);
    }
}