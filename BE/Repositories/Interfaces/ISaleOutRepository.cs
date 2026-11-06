using BE.Models.SaleOut;
using BE.Models.Common;

namespace BE.Repositories.Interfaces;

public interface ISaleOutRepository
{
    Task<PagedResult<SaleOutOrder>> SearchAsync(SaleOutSearchCriteria criteria);
    Task<PagedResult<SaleOutOrder>> SearchPrintOptionsAsync(string? searchText, long offset, int pageSize);
    Task<PagedResult<SaleOutRevenueReportItem>> RevenueReportAsync(DateOnly timeBegin, DateOnly timeEnd, long offset, int pageSize);
    Task<string?> CreateAsync(IReadOnlyList<SaleOut> saleOuts);
    Task<bool> UpdateAsync(Guid id, decimal quantity, decimal quantityPerBox, decimal price, decimal amount, decimal boxQuantity);
    Task<IReadOnlyList<SaleOut>> GetOrderAsync(string saleOutNo);
    Task<bool> UpdateOrderAsync(string saleOutNo, IReadOnlyList<SaleOut> saleOuts);
    Task<bool> SoftDeleteAsync(Guid id);
    Task<bool> SoftDeleteOrderAsync(string saleOutNo);
    Task<IReadOnlyList<SaleOutImportDuplicate>> ImportAsync(IReadOnlyList<SaleOut> saleOuts);
}
