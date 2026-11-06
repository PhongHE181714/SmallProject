using BE.DTOs.Database;

namespace BE.Services.Interfaces;

public interface IDatabaseService
{
    Task<DatabasePingResponse> PingAsync();
}
