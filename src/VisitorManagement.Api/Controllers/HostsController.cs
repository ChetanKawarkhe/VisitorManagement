using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagement.Application.DTOs.Lookups;
using VisitorManagement.Application.Interfaces;

namespace VisitorManagement.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,User")]
[Route("api/hosts")]
public sealed class HostsController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public HostsController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<HostResponse>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var hosts = await _lookupService.SearchHostsAsync(
            search,
            cancellationToken);

        return Ok(hosts);
    }
}