using Topstar.Application;
using Topstar.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddTopstarApplication();
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

app.MapPost("/api/v1/social-sync/koc/self", async (Guid requesterUserId, Topstar.Application.SocialSync.ISocialSyncOrchestrator orchestrator, CancellationToken cancellationToken) =>
{
    var result = await orchestrator.RequestKocSelfSyncAsync(requesterUserId, cancellationToken);
    return Results.Ok(result);
});

app.MapPost("/api/v1/social-sync/admin/all-kocs", async (Guid requesterUserId, Topstar.Application.SocialSync.ISocialSyncOrchestrator orchestrator, CancellationToken cancellationToken) =>
{
    var result = await orchestrator.RequestAdminSyncAllKocsAsync(requesterUserId, cancellationToken);
    return Results.Ok(result);
});

app.MapPost("/api/v1/social-sync/admin/koc/{kocProfileId:guid}", async (Guid requesterUserId, Guid kocProfileId, Topstar.Application.SocialSync.ISocialSyncOrchestrator orchestrator, CancellationToken cancellationToken) =>
{
    var result = await orchestrator.RequestAdminSyncSingleKocAsync(requesterUserId, kocProfileId, cancellationToken);
    return Results.Ok(result);
});

app.MapPost("/api/v1/social-sync/brand/campaign/{campaignId:guid}", async (Guid requesterUserId, Guid campaignId, Topstar.Application.SocialSync.ISocialSyncOrchestrator orchestrator, CancellationToken cancellationToken) =>
{
    var result = await orchestrator.RequestBrandCampaignSyncAsync(requesterUserId, campaignId, cancellationToken);
    return Results.Ok(result);
});

app.Run();
