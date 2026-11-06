
namespace BE.Services.Products;

internal static class ProductValidation
{
    public const int TEXT_LENGTH_50 = 50;
    public const int TEXT_LENGTH_200 = 200;

    private const int MAX_DECIMAL_SCALE = 3;
    private const decimal MAX_DECIMAL_18_3 = 999_999_999_999_999.999m;

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
