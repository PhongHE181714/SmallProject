namespace BE.DTOs.SaleOuts;

public static class SaleOutImportLimits
{
    public const long MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;
    public const int MAX_DATA_ROWS = 10_000;
    public const long MULTIPART_OVERHEAD_BYTES = 64 * 1024;
    public const long MAX_REQUEST_SIZE_BYTES = MAX_FILE_SIZE_BYTES + MULTIPART_OVERHEAD_BYTES;
}
