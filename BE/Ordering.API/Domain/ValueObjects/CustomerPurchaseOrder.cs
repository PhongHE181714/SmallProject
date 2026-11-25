using BE.BuildingBlocks.SharedKernel;

namespace BE.Services.OrderingService.Domain.ValueObjects;

public sealed class CustomerPurchaseOrder : ValueObject
{
    public const int MAX_PO_NO_LENGTH = 50;
    public const int MAX_CUSTOMER_NAME_LENGTH = 200;

    public string CustomerPoNo { get; }

    public string CustomerName { get; }

    public DateOnly OrderDate { get; }

    private CustomerPurchaseOrder(string customerPoNo, string customerName, DateOnly orderDate)
    {
        CustomerPoNo = customerPoNo;
        CustomerName = customerName;
        OrderDate = orderDate;
    }

    public static CustomerPurchaseOrder Create(
        string? customerPoNo,
        string? customerName,
        DateOnly? orderDate)
    {
        var normalizedPoNo = NormalizeRequiredText(customerPoNo, MAX_PO_NO_LENGTH, "Số PO khách hàng");
        var normalizedCustomerName = NormalizeRequiredText(customerName, MAX_CUSTOMER_NAME_LENGTH, "Khách hàng");
        var validatedOrderDate = orderDate
            ?? throw new ArgumentException("Ngày đặt hàng không được để trống.");

        return new CustomerPurchaseOrder(normalizedPoNo, normalizedCustomerName, validatedOrderDate);
    }

    public static string NormalizeRequiredText(string? value, int maxLength, string displayName)
    {
        var normalizedValue = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            throw new ArgumentException($"{displayName} không được để trống.");
        }

        if (normalizedValue.Length > maxLength)
        {
            throw new ArgumentException($"{displayName} không được vượt quá {maxLength} ký tự.");
        }

        return normalizedValue;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CustomerPoNo;
        yield return CustomerName;
        yield return OrderDate;
    }
}
