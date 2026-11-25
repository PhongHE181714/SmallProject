using BE.BuildingBlocks.CQRS;

namespace BE.Services.CatalogService.Application.Queries.GenerateProductTemplate;

public sealed record GenerateProductTemplateQuery : IQuery<byte[]>;
