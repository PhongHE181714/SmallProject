using BE.BuildingBlocks.CQRS;
using BE.Services.CatalogService.Application.Services;

namespace BE.Services.CatalogService.Application.Queries.GenerateProductTemplate;

public sealed class GenerateProductTemplateQueryHandler : IQueryHandler<GenerateProductTemplateQuery, byte[]>
{
    private readonly IProductSpreadsheetService _spreadsheetService;

    public GenerateProductTemplateQueryHandler(IProductSpreadsheetService spreadsheetService)
    {
        _spreadsheetService = spreadsheetService;
    }

    public Task<byte[]> HandleAsync(
        GenerateProductTemplateQuery query,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_spreadsheetService.GenerateTemplate());
    }
}
