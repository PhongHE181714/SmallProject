using BE.ApiGateway.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiGateway(builder.Configuration);

var app = builder.Build();

app.UseApiGateway();

app.Run();
