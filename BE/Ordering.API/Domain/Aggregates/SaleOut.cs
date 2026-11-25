using BE.BuildingBlocks.SharedKernel;
using BE.Services.OrderingService.Domain.ProductReplica;
using BE.Services.OrderingService.Domain.ValueObjects;

namespace BE.Services.OrderingService.Domain.Aggregates;

public sealed class SaleOut : Entity<Guid>
{
    public const int DECIMAL_SCALE = 3;
    public const decimal MAX_DECIMAL_18_3 = 999_999_999_999_999.999m;

    public string? SaleOutNo { get; init; }

    public string? CustomerPoNo { get; init; }

    public DateTime? OrderDate { get; init; }

    public string? CustomerName { get; init; }

    public Guid? ProductId { get; init; }

    public string? ProductCode { get; init; }

    public string? ProductName { get; init; }

    public string? Unit { get; init; }

    public decimal? Quantity { get; init; }

    public decimal? Price { get; init; }

    public decimal? Amount { get; init; }

    public decimal? QuantityPerBox { get; init; }

    public decimal? BoxQuantity { get; init; }

    public bool IsActive { get; init; }

    public static SaleOut CreateLine(
        CustomerPurchaseOrder purchaseOrder,
        ProductSnapshot product,
        decimal? quantityValue,
        decimal? quantityPerBoxValue,
        decimal? priceValue,
        Guid id)
    {
        var quantity = ValidateInteger(quantityValue, "Số lượng", allowZero: false);
        var price = ValidateInteger(priceValue, "Đơn giá", allowZero: true);
        var quantityPerBox = ValidateInteger(
            quantityPerBoxValue ?? product.QuantityPerBox,
            "Số lượng/thùng",
            allowZero: false);

        var (amount, boxQuantity) = CalculateTotals(quantity, price, quantityPerBox);

        return new SaleOut
        {
            Id = id,
            CustomerPoNo = purchaseOrder.CustomerPoNo,
            OrderDate = purchaseOrder.OrderDate.ToDateTime(TimeOnly.MinValue),
            CustomerName = purchaseOrder.CustomerName,
            ProductId = product.Id,
            ProductCode = product.ProductCode,
            ProductName = product.ProductName,
            Unit = product.Unit,
            Quantity = quantity,
            Price = price,
            Amount = amount,
            QuantityPerBox = quantityPerBox,
            BoxQuantity = boxQuantity,
            IsActive = true
        };
    }

    public static (decimal Quantity, decimal QuantityPerBox, decimal Price, decimal Amount, decimal BoxQuantity) ValidateLineUpdate(
        decimal? quantityValue,
        decimal? quantityPerBoxValue,
        decimal? priceValue)
    {
        var quantity = ValidateDecimal(quantityValue, "Số lượng", allowZero: false);
        if (quantity != decimal.Truncate(quantity))
        {
            throw new ArgumentException("Số lượng phải là số nguyên.");
        }

        var quantityPerBox = ValidateDecimal(quantityPerBoxValue, "Số lượng/thùng", allowZero: false);
        if (quantityPerBox != decimal.Truncate(quantityPerBox))
        {
            throw new ArgumentException("Số lượng/thùng phải là số nguyên dương.");
        }

        var price = ValidateDecimal(priceValue, "Đơn giá", allowZero: true);
        if (price != decimal.Truncate(price))
        {
            throw new ArgumentException("Đơn giá phải là số nguyên.");
        }

        var (amount, boxQuantity) = CalculateTotals(quantity, price, quantityPerBox);
        return (quantity, quantityPerBox, price, amount, boxQuantity);
    }

    public static (decimal Amount, decimal BoxQuantity) CalculateTotals(
        decimal quantity,
        decimal price,
        decimal quantityPerBox)
    {
        if (price > 0 && quantity > MAX_DECIMAL_18_3 / price)
        {
            throw new ArgumentException("Thành tiền vượt quá giới hạn lưu trữ.");
        }

        var amount = decimal.Round(quantity * price, DECIMAL_SCALE, MidpointRounding.AwayFromZero);
        var boxQuantity = decimal.Ceiling(quantity / quantityPerBox);
        if (amount > MAX_DECIMAL_18_3 || boxQuantity > MAX_DECIMAL_18_3)
        {
            throw new ArgumentException("Giá trị tính toán vượt quá giới hạn lưu trữ.");
        }

        return (amount, boxQuantity);
    }

    private static decimal ValidateInteger(decimal? value, string displayName, bool allowZero)
    {
        var numericValue = ValidateDecimal(value, displayName, allowZero);
        if (numericValue != decimal.Truncate(numericValue))
        {
            throw new ArgumentException($"{displayName} phải là số nguyên.");
        }

        return numericValue;
    }

    private static decimal ValidateDecimal(decimal? value, string displayName, bool allowZero)
    {
        if (value is null)
        {
            throw new ArgumentException($"{displayName} không được để trống.");
        }

        var numericValue = value.Value;
        if (numericValue < 0 || (!allowZero && numericValue == 0))
        {
            var condition = allowZero ? "không được là số âm" : "phải lớn hơn 0";
            throw new ArgumentException($"{displayName} {condition}.");
        }

        if (numericValue > MAX_DECIMAL_18_3 || decimal.Round(numericValue, DECIMAL_SCALE) != numericValue)
        {
            throw new ArgumentException(
                $"{displayName} phải có tối đa {DECIMAL_SCALE} chữ số thập phân và không vượt quá giới hạn lưu trữ.");
        }

        return numericValue;
    }
}
