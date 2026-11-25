using BE.BuildingBlocks.CQRS;
using BE.Services.OrderingService.Application.Services;

namespace BE.Services.OrderingService.Application.Queries.GenerateSaleOutTemplate;

public sealed class GenerateSaleOutTemplateQueryHandler : IQueryHandler<GenerateSaleOutTemplateQuery, byte[]>
{
    private readonly ISaleOutDocumentService _documentService;

    public GenerateSaleOutTemplateQueryHandler(ISaleOutDocumentService documentService)
    {
        _documentService = documentService;
    }

    public Task<byte[]> HandleAsync(
        GenerateSaleOutTemplateQuery query,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_documentService.GenerateImportTemplate());
    }
}
