using Topstar.Application;
using Topstar.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddTopstarInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/v1/system/platform-info", (IPlatformInfoService service) =>
{
    return Results.Ok(service.GetInfo());
})
.WithName("GetPlatformInfo");

app.Run();
