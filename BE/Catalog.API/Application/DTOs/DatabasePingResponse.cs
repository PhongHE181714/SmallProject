namespace BE.Services.CatalogService.Application.DTOs;

public sealed class DatabasePingResponse
{
    public bool Connected { get; init; }

    public required string Database { get; init; }
}
