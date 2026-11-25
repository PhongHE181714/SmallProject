using BE.BuildingBlocks.CQRS;

namespace BE.Services.OrderingService.Application.Queries.GenerateSaleOutTemplate;

public sealed record GenerateSaleOutTemplateQuery : IQuery<byte[]>;
