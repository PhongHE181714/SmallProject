namespace BE.Repositories.Interfaces;

public interface IDatabaseRepository
{
    Task<string> GetDatabaseNameAsync();
}
