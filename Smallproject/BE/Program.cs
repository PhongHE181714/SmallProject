using BE.Data;
using BE.Data.Interfaces;
using BE.Repositories;
using BE.Services;
using BE.Repositories.Interfaces;
using BE.Repositories.Products;
using BE.Repositories.SaleOuts;
using BE.Services.Interfaces;
using BE.Services.Products;
using BE.Services.SaleOuts;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var invalidFields = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .Select(entry => entry.Key)
            .ToArray();

        var messages = new List<string>();
        if (invalidFields.Any(field => field.Contains("quantityPerBox", StringComparison.OrdinalIgnoreCase)))
        {
            messages.Add("Số lượng/thùng phải là số nguyên không âm, ví dụ 24.");
        }

        if (invalidFields.Any(field => field.Contains("productWeight", StringComparison.OrdinalIgnoreCase)))
        {
            messages.Add("Trọng lượng phải là số hợp lệ; có thể nhập số thập phân như 0.6.");
        }

        if (messages.Count == 0)
        {
            messages.Add(invalidFields.Any(field => field.Equals("request", StringComparison.OrdinalIgnoreCase))
                ? "Vui lòng gửi thông tin sản phẩm bằng JSON hợp lệ."
                : "Một hoặc nhiều trường có định dạng không hợp lệ. Hãy kiểm tra dữ liệu rồi thử lại.");
        }

        return new BadRequestObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Dữ liệu gửi lên chưa hợp lệ",
            Detail = string.Join(" ", messages.Distinct())
        });
    };
});
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IDatabaseRepository, DatabaseRepository>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISaleOutRepository, SaleOutRepository>();
builder.Services.AddScoped<ISaleOutService, SaleOutService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
