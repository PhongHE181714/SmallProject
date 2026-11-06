using BE.DTOs.SaleOuts;
using BE.Models.SaleOut;
using BE.Repositories.Interfaces;
using BE.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace BE.Services.SaleOuts;

public sealed partial class SaleOutService : ISaleOutService
{
    private const int TEXT_LENGTH_50 = 50;
    private const int TEXT_LENGTH_200 = 200;
    private const int DECIMAL_SCALE = 3;
    private const decimal MAX_DECIMAL_18_3 = 999_999_999_999_999.999m;

    private readonly ISaleOutRepository _saleOutRepository;
    private readonly IProductRepository _productRepository;

    public SaleOutService(ISaleOutRepository saleOutRepository, IProductRepository productRepository)
    {
        _saleOutRepository = saleOutRepository;
        _productRepository = productRepository;
    }

    public async Task<SaleOutCreateResponse> CreateAsync(CreateSaleOutRequest request)
    {
        if (request is null || request.Items is null || request.Items.Count == 0)
        {
            throw new ArgumentException("Đơn hàng phải có ít nhất một sản phẩm.", nameof(request));
        }

        var customerPoNo = NormalizeRequiredText(request.CustomerPoNo, TEXT_LENGTH_50, "Số PO khách hàng");
        var customerName = NormalizeRequiredText(request.CustomerName, TEXT_LENGTH_200, "Khách hàng");
        var orderDate = request.OrderDate
            ?? throw new ArgumentException("Ngày đặt hàng không được để trống.");

        EnsureDistinctProducts(request.Items.Select(item => item.ProductId));
        var saleOuts = new List<SaleOut>(request.Items.Count);
        foreach (var item in request.Items)
        {
            saleOuts.Add(await CreateSaleOutLineAsync(
                customerPoNo,
                customerName,
                orderDate,
                item.ProductId,
                item.Quantity,
                item.QuantityPerBox,
                item.Price,
                Guid.NewGuid()));
        }

        var saleOutNo = await _saleOutRepository.CreateAsync(saleOuts);
        if (saleOutNo is null)
        {
            throw new InvalidOperationException(
                $"Số PO khách hàng '{customerPoNo}' đã tồn tại. Hãy mở đơn đó để chỉnh sửa danh sách sản phẩm.");
        }

        return new SaleOutCreateResponse
        {
            SaleOutNo = saleOutNo,
            CustomerPoNo = customerPoNo,
            OrderDate = orderDate,
            CustomerName = customerName,
            ItemCount = saleOuts.Count
        };
    }

    public Task<IReadOnlyList<SaleOutListItemResponse>> GetOrderAsync(string saleOutNo)
    {
        var normalizedSaleOutNo = NormalizeRequiredText(saleOutNo, TEXT_LENGTH_50, "Mã số phiếu");
        return GetOrderItemsAsync(normalizedSaleOutNo);
    }

    private async Task<IReadOnlyList<SaleOutListItemResponse>> GetOrderItemsAsync(string saleOutNo)
    {
        var saleOuts = await _saleOutRepository.GetOrderAsync(saleOutNo);
        return saleOuts.Select(MapSaleOutProduct).ToArray();
    }

    private static SaleOutListItemResponse MapSaleOutProduct(SaleOut saleOut)
    {
        return new SaleOutListItemResponse
        {
            Id = saleOut.Id,
            SaleOutNo = saleOut.SaleOutNo,
            CustomerPoNo = saleOut.CustomerPoNo,
            OrderDate = saleOut.OrderDate is { } orderDate ? DateOnly.FromDateTime(orderDate) : null,
            CustomerName = saleOut.CustomerName,
            ProductId = saleOut.ProductId,
            ProductCode = saleOut.ProductCode,
            ProductName = saleOut.ProductName,
            Unit = saleOut.Unit,
            Quantity = saleOut.Quantity,
            Price = saleOut.Price,
            Amount = saleOut.Amount,
            QuantityPerBox = saleOut.QuantityPerBox,
            BoxQuantity = saleOut.BoxQuantity,
            IsActive = saleOut.IsActive
        };
    }

    public async Task<bool> UpdateOrderAsync(string saleOutNo, UpdateSaleOutOrderRequest request)
    {
        var normalizedSaleOutNo = NormalizeRequiredText(saleOutNo, TEXT_LENGTH_50, "Mã số phiếu");
        if (request?.Items is null || request.Items.Count == 0)
        {
            throw new ArgumentException("Đơn hàng phải có ít nhất một sản phẩm.");
        }

        var existingItems = await _saleOutRepository.GetOrderAsync(normalizedSaleOutNo);
        if (existingItems.Count == 0)
        {
            return false;
        }

        var existingById = existingItems.ToDictionary(item => item.Id);
        var requestedIds = request.Items.Where(item => item.Id.HasValue).Select(item => item.Id!.Value).ToArray();
        if (requestedIds.Distinct().Count() != requestedIds.Length
            || requestedIds.Any(id => !existingById.ContainsKey(id)))
        {
            throw new ArgumentException("Danh sách sản phẩm chứa dòng không thuộc đơn hàng này.");
        }

        EnsureDistinctProducts(request.Items.Select(item => item.ProductId));
        var firstItem = existingItems[0];
        var orderDate = firstItem.OrderDate is { } date ? DateOnly.FromDateTime(date) : DateOnly.FromDateTime(DateTime.Today);
        var saleOuts = new List<SaleOut>(request.Items.Count);
        foreach (var item in request.Items)
        {
            saleOuts.Add(await CreateSaleOutLineAsync(
                firstItem.CustomerPoNo,
                firstItem.CustomerName,
                orderDate,
                item.ProductId,
                item.Quantity,
                item.QuantityPerBox,
                item.Price,
                item.Id ?? Guid.Empty));
        }

        try
        {
            return await _saleOutRepository.UpdateOrderAsync(normalizedSaleOutNo, saleOuts);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            throw new InvalidOperationException("Sản phẩm đã tồn tại trong một đơn hàng khác hoặc đã được lưu trước đó.", exception);
        }
    }

    private async Task<SaleOut> CreateSaleOutLineAsync(
        string? customerPoNo,
        string? customerName,
        DateOnly orderDate,
        Guid? productIdValue,
        decimal? quantityValue,
        decimal? quantityPerBoxValue,
        decimal? priceValue,
        Guid id)
    {
        var productId = productIdValue ?? throw new ArgumentException("Vui lòng chọn sản phẩm.");
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Vui lòng chọn sản phẩm hợp lệ.");
        }

        var product = await _productRepository.FindActiveByIdAsync(productId)
            ?? throw new KeyNotFoundException("Không tìm thấy sản phẩm đang hoạt động.");
        var quantity = ValidateInteger(quantityValue, "Số lượng", allowZero: false);
        var price = ValidateInteger(priceValue, "Đơn giá", allowZero: true);
        var quantityPerBox = ValidateInteger(quantityPerBoxValue ?? product.QuantityPerBox, "Số lượng/thùng", allowZero: false);

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

        return new SaleOut
        {
            Id = id,
            CustomerPoNo = customerPoNo,
            OrderDate = orderDate.ToDateTime(TimeOnly.MinValue),
            CustomerName = customerName,
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

    private static decimal ValidateInteger(decimal? value, string displayName, bool allowZero)
    {
        var numericValue = ValidateDecimal(value, displayName, allowZero);
        if (numericValue != decimal.Truncate(numericValue))
        {
            throw new ArgumentException($"{displayName} phải là số nguyên.");
        }

        return numericValue;
    }

    private static void EnsureDistinctProducts(IEnumerable<Guid?> productIdValues)
    {
        var productIds = productIdValues.Select(id => id ?? Guid.Empty).ToArray();
        if (productIds.Any(id => id == Guid.Empty) || productIds.Distinct().Count() != productIds.Length)
        {
            throw new ArgumentException("Mỗi sản phẩm chỉ được xuất hiện một lần trong cùng đơn hàng.");
        }
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateSaleOutRequest request)
    {
        if (id == Guid.Empty || request is null)
        {
            throw new ArgumentException("Thông tin đơn xuất hàng chưa hợp lệ.");
        }

        var quantity = ValidateDecimal(request.Quantity, "Số lượng", allowZero: false);
        if (quantity != decimal.Truncate(quantity))
        {
            throw new ArgumentException("Số lượng phải là số nguyên.");
        }

        var quantityPerBox = ValidateDecimal(request.QuantityPerBox, "Số lượng/thùng", allowZero: false);
        if (quantityPerBox != decimal.Truncate(quantityPerBox))
        {
            throw new ArgumentException("Số lượng/thùng phải là số nguyên dương.");
        }

        var price = ValidateDecimal(request.Price, "Đơn giá", allowZero: true);
        if (price != decimal.Truncate(price))
        {
            throw new ArgumentException("Đơn giá phải là số nguyên.");
        }

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

        return await _saleOutRepository.UpdateAsync(id, quantity, quantityPerBox, price, amount, boxQuantity);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Mã đơn xuất hàng không hợp lệ.", nameof(id));
        }

        return _saleOutRepository.SoftDeleteAsync(id);
    }

    public Task<bool> DeleteOrderAsync(string saleOutNo)
    {
        var normalizedSaleOutNo = NormalizeRequiredText(saleOutNo, TEXT_LENGTH_50, "Mã số phiếu");
        return _saleOutRepository.SoftDeleteOrderAsync(normalizedSaleOutNo);
    }

    public async Task<SaleOutSearchResponse> SearchAsync(SaleOutSearchRequest request)
    {
        var criteria = CreateSearchCriteria(request);
        var result = await _saleOutRepository.SearchAsync(criteria);
        var totalPages = result.TotalItems == 0
            ? 0
            : (result.TotalItems + request.PageSize - 1) / request.PageSize;

        return new SaleOutSearchResponse
        {
            Items = result.Items.Select(order => new SaleOutOrderListItemResponse
            {
                SaleOutNo = order.SaleOutNo,
                CustomerPoNo = order.CustomerPoNo,
                OrderDate = order.OrderDate is { } orderDate
                    ? DateOnly.FromDateTime(orderDate)
                    : null,
                CustomerName = order.CustomerName,
                ProductCount = order.ProductCount,
                TotalAmount = order.TotalAmount,
                Products = order.Products.Select(MapSaleOutProduct).ToArray()
            }).ToArray(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalItems = result.TotalItems,
            TotalPages = totalPages
        };
    }

    private static SaleOutSearchCriteria CreateSearchCriteria(SaleOutSearchRequest request)
    {
        var searchText = request.SearchValue?.Trim();
        SaleOutSearchField? searchField = null;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            if (string.IsNullOrWhiteSpace(request.SearchField))
            {
                throw new ArgumentException("Cần chọn searchField khi có searchValue.");
            }

            searchField = ParseSearchField(request.SearchField);
        }
        else
        {
            searchText = null;
        }

        return new SaleOutSearchCriteria
        {
            SearchField = searchField,
            SearchText = searchText,
            Offset = (long)(request.Page - 1) * request.PageSize,
            PageSize = request.PageSize
        };
    }

    private static SaleOutSearchField ParseSearchField(string searchField)
    {
        return searchField.Trim().ToUpperInvariant() switch
        {
            "CUSTOMERPONO" => SaleOutSearchField.CustomerPoNo,
            "SALEOUTNO" => SaleOutSearchField.SaleOutNo,
            "ORDERDATE" => SaleOutSearchField.OrderDate,
            "CUSTOMERNAME" => SaleOutSearchField.CustomerName,
            "PRODUCTCODE" => SaleOutSearchField.ProductCode,
            "PRODUCTNAME" => SaleOutSearchField.ProductName,
            _ => throw new ArgumentException(
                "searchField không hợp lệ. Hãy chọn một trường tìm kiếm Sale Out được hỗ trợ.")
        };
    }

    private static string NormalizeRequiredText(string? value, int maxLength, string displayName)
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
