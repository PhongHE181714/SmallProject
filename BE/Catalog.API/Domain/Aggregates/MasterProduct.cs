using BE.BuildingBlocks.SharedKernel;
using BE.Services.CatalogService.Domain.Events;

namespace BE.Services.CatalogService.Domain.Aggregates;

public sealed class MasterProduct : AggregateRoot<Guid>
{
    public const int TEXT_LENGTH_50 = 50;
    public const int TEXT_LENGTH_200 = 200;
    public const int MAX_DECIMAL_SCALE = 3;
    public const decimal MAX_DECIMAL_18_3 = 999_999_999_999_999.999m;

    public string? ProductCode { get; private set; }

    public string? ProductName { get; private set; }

    public string? Unit { get; private set; }

    public string? Specification { get; private set; }

    public decimal? QuantityPerBox { get; private set; }

    public decimal? ProductWeight { get; private set; }

    public bool IsActive { get; private set; }

    public MasterProduct()
    {
    }

    public static MasterProduct Create(
        string? productCode,
        string? productName,
        string? unit,
        string? specification,
        int? quantityPerBox,
        decimal? productWeight)
    {
        return CreateInternal(
            Guid.NewGuid(),
            productCode,
            productName,
            unit,
            specification,
            quantityPerBox,
            productWeight,
            isActive: true);
    }

    public static MasterProduct UpdateExisting(
        Guid id,
        string? productCode,
        string? productName,
        string? unit,
        string? specification,
        int? quantityPerBox,
        decimal? productWeight,
        bool isActive)
    {
        return CreateInternal(
            id,
            productCode,
            productName,
            unit,
            specification,
            quantityPerBox,
            productWeight,
            isActive);
    }

    public static MasterProduct CreateValidatedForImport(
        string productCode,
        string productName,
        string unit,
        string specification,
        decimal quantityPerBox,
        decimal productWeight)
    {
        var product = new MasterProduct
        {
            Id = Guid.NewGuid(),
            ProductCode = productCode,
            ProductName = productName,
            Unit = unit,
            Specification = specification,
            QuantityPerBox = quantityPerBox,
            ProductWeight = productWeight,
            IsActive = true
        };

        product.AddDomainEvent(new ProductUpsertedDomainEvent(
            product.Id,
            productCode,
            productName,
            unit,
            specification,
            quantityPerBox,
            productWeight,
            product.IsActive));

        return product;
    }

    private static MasterProduct CreateInternal(
        Guid id,
        string? productCode,
        string? productName,
        string? unit,
        string? specification,
        int? quantityPerBox,
        decimal? productWeight,
        bool isActive)
    {
        var normalizedCode = NormalizeRequiredText(productCode, TEXT_LENGTH_50, "Mã sản phẩm");
        var normalizedName = NormalizeRequiredText(productName, TEXT_LENGTH_200, "Tên sản phẩm");
        var normalizedUnit = NormalizeRequiredText(unit, TEXT_LENGTH_50, "Đơn vị");
        var normalizedSpecification = NormalizeRequiredText(specification, TEXT_LENGTH_200, "Quy cách");
        var validatedQuantityPerBox = ValidateRequiredQuantityPerBox(quantityPerBox);
        var validatedWeight = ValidateRequiredDecimal(productWeight, "Trọng lượng");

        var product = new MasterProduct
        {
            Id = id,
            ProductCode = normalizedCode,
            ProductName = normalizedName,
            Unit = normalizedUnit,
            Specification = normalizedSpecification,
            QuantityPerBox = validatedQuantityPerBox,
            ProductWeight = validatedWeight,
            IsActive = isActive
        };

        product.AddDomainEvent(new ProductUpsertedDomainEvent(
            product.Id,
            normalizedCode,
            normalizedName,
            normalizedUnit,
            normalizedSpecification,
            validatedQuantityPerBox,
            validatedWeight,
            isActive));

        return product;
    }

    public static string NormalizeRequiredText(string? value, int maxLength, string fieldName)
    {
        var normalizedValue = value?.Trim();
        if (string.IsNullOrEmpty(normalizedValue))
        {
            throw new ArgumentException($"{fieldName} là bắt buộc.");
        }

        if (normalizedValue.Length > maxLength)
        {
            throw new ArgumentException($"{fieldName} không được vượt quá {maxLength} ký tự.");
        }

        return normalizedValue;
    }

    public static decimal ValidateRequiredDecimal(decimal? value, string fieldName)
    {
        if (value is null)
        {
            throw new ArgumentException($"{fieldName} là bắt buộc.");
        }

        if (value.Value <= 0 || value.Value > MAX_DECIMAL_18_3)
        {
            throw new ArgumentException($"{fieldName} phải lớn hơn hoặc bằng 0.");
        }

        if (decimal.Round(value.Value, MAX_DECIMAL_SCALE) != value.Value)
        {
            throw new ArgumentException($"{fieldName} chỉ được có tối đa {MAX_DECIMAL_SCALE} chữ số thập phân.");
        }

        return value.Value;
    }

    public static decimal ValidateRequiredQuantityPerBox(int? value)
    {
        if (value is null)
        {
            throw new ArgumentException("Số lượng/thùng là bắt buộc.");
        }

        if (value.Value <= 0)
        {
            throw new ArgumentException("Số lượng/thùng phải là số nguyên không âm.");
        }

        return value.Value;
    }
}
