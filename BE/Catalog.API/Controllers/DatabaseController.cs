using BE.BuildingBlocks.CQRS;
using BE.Services.CatalogService.Application.DTOs;
using BE.Services.CatalogService.Application.Queries.PingDatabase;
using Microsoft.AspNetCore.Mvc;

namespace BE.Services.CatalogService.Controllers;

[ApiController]
[Route("api/database")]
public sealed class DatabaseController : ControllerBase
{
    private readonly ISender _sender;

    public DatabaseController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("ping")]
    public async Task<IActionResult> Ping(CancellationToken cancellationToken)
    {
        var response = await _sender.SendQueryAsync<PingDatabaseQuery, DatabasePingResponse>(
            new PingDatabaseQuery(),
            cancellationToken);
        return Ok(response);
    }
}
