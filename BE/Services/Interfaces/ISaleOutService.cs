using BE.DTOs.SaleOuts;

namespace BE.Services.Interfaces;

public interface ISaleOutService
{
    Task<SaleOutSearchResponse> SearchAsync(SaleOutSearchRequest request);
    Task<SaleOutPrintOptionsResponse> SearchPrintOptionsAsync(SaleOutPrintOptionsRequest request);
    Task<byte[]?> GeneratePrintPdfAsync(string saleOutNo);
    Task<byte[]> GenerateRevenueReportAsync(DateOnly timeBegin, DateOnly timeEnd);
    Task<SaleOutCreateResponse> CreateAsync(CreateSaleOutRequest request);
    Task<IReadOnlyList<SaleOutListItemResponse>> GetOrderAsync(string saleOutNo);
    Task<bool> UpdateOrderAsync(string saleOutNo, UpdateSaleOutOrderRequest request);
    Task<bool> UpdateAsync(Guid id, UpdateSaleOutRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> DeleteOrderAsync(string saleOutNo);
    Task<SaleOutImportResponse> ImportAsync(Stream fileStream, string fileExtension);
    byte[] GenerateImportTemplate();
}
