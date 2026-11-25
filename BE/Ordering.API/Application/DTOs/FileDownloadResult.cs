namespace BE.Services.OrderingService.Application.DTOs;

public sealed record FileDownloadResult(byte[] FileBytes, string ContentType, string FileName);
