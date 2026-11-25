namespace BE.Services.CatalogService.Domain.ValueObjects;

public sealed class ProductImportDuplicate
{
    public string FieldName { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
