using BE.BuildingBlocks.CQRS;
using BE.Services.CatalogService.Application.DTOs;

namespace BE.Services.CatalogService.Application.Queries.PingDatabase;

public sealed record PingDatabaseQuery : IQuery<DatabasePingResponse>;
