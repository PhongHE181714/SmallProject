namespace BE.Services.CatalogService.Domain.Repositories;

public interface IDatabaseRepository
{
    Task<string> GetDatabaseNameAsync();
}
