namespace BE.Services.OrderingService.Domain.ProductReplica;

public interface IProductSnapshotRepository
{
    Task<ProductSnapshot?> FindActiveByIdAsync(Guid productId);
    Task<IReadOnlyList<ProductSnapshot>> FindActiveByCodesAsync(IReadOnlyCollection<string> productCodes);
    void UpsertSnapshot(ProductSnapshot snapshot);
    void RemoveSnapshot(Guid productId);
}
