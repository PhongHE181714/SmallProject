using BE.Models.MasterProduct;
using BE.Models.Common;

namespace BE.Repositories.Interfaces;

public interface IProductRepository
{
    Task<PagedResult<MasterProduct>> SearchAsync(ProductSearchCriteria criteria);
    Task<MasterProduct?> FindActiveByIdAsync(Guid productId);
    Task<IReadOnlyList<MasterProduct>> FindActiveByCodesAsync(IReadOnlyCollection<string> productCodes);
    Task<IReadOnlyList<string>> FindDuplicateFieldsAsync(string? productCode, string? productName);
    Task CreateAsync(MasterProduct product);
    Task UpdateAsync(MasterProduct product);
    Task<IReadOnlyList<string>> FindDuplicateFieldsForUpdateAsync(
        Guid currentId,
        string? productCode,
        string? productName);
    Task<bool> ExistsAsync(Guid productId);
    Task<bool> HasRelatedTransactionsAsync(Guid productId);
    Task<bool> DeleteAsync(Guid productId);
    Task<IReadOnlyList<ProductImportDuplicate>> ImportAsync(IReadOnlyList<MasterProduct> products);
}
