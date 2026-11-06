namespace BE.DTOs.Database;

public sealed class DatabasePingResponse
{
    public bool Connected { get; init; }

    public required string Database { get; init; }
}
