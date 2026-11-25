using BE.BuildingBlocks.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMicroserviceHost(builder.Configuration, typeof(Program).Assembly);

var app = builder.Build();

app.UseMicroserviceHost();

app.Run();
