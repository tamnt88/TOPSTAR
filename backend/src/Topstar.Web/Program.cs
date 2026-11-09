using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Topstar.Application;
using Topstar.Infrastructure;
using Topstar.Web.Services.Email;
using Topstar.Web.Services.Storage;
using Topstar.Web.Services.Payments;
using Topstar.Web.Services.Profiles;

var contentRoot = ResolveContentRoot();
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = contentRoot,
    WebRootPath = Path.Combine(contentRoot, "wwwroot")
});

LoadSocialLoginSettings(builder.Configuration, builder.Environment.IsDevelopment());

builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("registration", context =>
        !HttpMethods.IsPost(context.Request.Method)
            ? RateLimitPartition.GetNoLimiter("registration-read")
            : RateLimitPartition.GetSlidingWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 3,
                    Window = TimeSpan.FromMinutes(30),
                    SegmentsPerWindow = 6,
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/dang-nhap";
        options.LogoutPath = "/dang-xuat";
        options.AccessDeniedPath = "/dang-nhap";
        options.Cookie.Name = "topstar.auth";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            var loginPath = context.Request.Path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase)
                ? "/admin/dang-nhap"
                : options.LoginPath.Value!;
            var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
            context.Response.Redirect($"{loginPath}?ReturnUrl={Uri.EscapeDataString(returnUrl)}");
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            var target = context.Request.Path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase)
                ? "/admin/dang-nhap"
                : options.AccessDeniedPath.Value!;
            context.Response.Redirect(target);
            return Task.CompletedTask;
        };
    })
    .AddCookie("External", options =>
    {
        options.Cookie.Name = "topstar.external";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    });

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleRedirectUri = builder.Configuration["Authentication:Google:RedirectUri"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication().AddGoogle("Google", options =>
    {
        options.SignInScheme = "External";
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = GetCallbackPath(googleRedirectUri, "/signin-google");
        options.CorrelationCookie.SameSite = SameSiteMode.None;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.IsEssential = true;
        options.Events.OnRemoteFailure = context =>
        {
            context.HandleResponse();
            context.Response.Redirect("/dang-nhap?externalError=oauth-session");
            return Task.CompletedTask;
        };
        if (!string.IsNullOrWhiteSpace(googleRedirectUri))
            options.Events.OnRedirectToAuthorizationEndpoint = context =>
            {
                var redirectUri = ResolveOAuthRedirectUri(context.Request, googleRedirectUri, "/signin-google");
                context.Response.Redirect(ReplaceQueryParameter(context.RedirectUri, "redirect_uri", redirectUri));
                return Task.CompletedTask;
            };
    });

    builder.Services.AddAuthentication().AddGoogle("GoogleYouTube", options =>
    {
        options.SignInScheme = "External";
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google-youtube";
        options.CorrelationCookie.SameSite = SameSiteMode.None;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.IsEssential = true;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("https://www.googleapis.com/auth/youtube.readonly");
        options.SaveTokens = true;
        options.AccessType = "offline";
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            var redirectUri = ResolveOAuthRedirectUri(context.Request, null, "/signin-google-youtube");
            var authorizationUrl = ReplaceQueryParameter(context.RedirectUri, "redirect_uri", redirectUri);
            var sep = authorizationUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            context.Response.Redirect($"{authorizationUrl}{sep}prompt=consent");
            return Task.CompletedTask;
        };
    });
}

var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
var facebookRedirectUri = builder.Configuration["Authentication:Facebook:RedirectUri"];
if (!string.IsNullOrWhiteSpace(facebookAppId) && !string.IsNullOrWhiteSpace(facebookAppSecret))
{
    builder.Services.AddAuthentication().AddFacebook("Facebook", options =>
    {
        options.SignInScheme = "External";
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
        options.CallbackPath = GetCallbackPath(facebookRedirectUri, "/signin-facebook");
        options.CorrelationCookie.SameSite = SameSiteMode.None;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.IsEssential = true;
        options.Events.OnRemoteFailure = context =>
        {
            context.HandleResponse();
            context.Response.Redirect("/dang-nhap?externalError=oauth-session");
            return Task.CompletedTask;
        };
        if (!string.IsNullOrWhiteSpace(facebookRedirectUri))
            options.Events.OnRedirectToAuthorizationEndpoint = context =>
            {
                var redirectUri = ResolveOAuthRedirectUri(context.Request, facebookRedirectUri, "/signin-facebook");
                context.Response.Redirect(ReplaceQueryParameter(context.RedirectUri, "redirect_uri", redirectUri));
                return Task.CompletedTask;
            };
    });
}

builder.Services.AddAuthorization();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AuthorizeFolder("/Brand");
    options.Conventions.AuthorizeFolder("/Koc");
    options.Conventions.AllowAnonymousToFolder("/Public");
});
builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options => options.Filters.Add<Topstar.Web.Features.Common.Paging.ListStatePageFilter>());
builder.Services.AddTopstarApplication();
builder.Services.AddTopstarInfrastructure(builder.Configuration);
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IEmailAccountDiagnostics, EmailAccountDiagnostics>();
builder.Services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();
builder.Services.AddScoped<ICampaignParticipationEmailService, CampaignParticipationEmailService>();
builder.Services.AddScoped<IEmailLogoEmbedder, EmailLogoEmbedder>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IKocProfileSectionService, KocProfileSectionService>();
builder.Services.AddScoped<IKocProfileViewService, KocProfileViewService>();
builder.Services.AddScoped<IOnePayService, OnePayService>();
builder.Services.AddScoped<VnPayService>();
builder.Services.AddScoped<Topstar.Infrastructure.Finance.BrandBalanceService>();
builder.Services.AddScoped<Topstar.Infrastructure.Campaigns.CampaignLifecycleService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<VnPayCallbackJournal>();

builder.Services.AddScoped<Topstar.Web.Services.TrackingConfigurationService>();
var app = builder.Build();

// OAuth correlation cookies are host-scoped. Always begin and finish the
// external-login round trip on the canonical apex host.
app.Use(async (context, next) =>
{
    if (context.Request.Host.Host.Equals("www.topstar.vn", StringComparison.OrdinalIgnoreCase))
    {
        var target = $"https://topstar.vn{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
        context.Response.Redirect(target, permanent: false);
        return;
    }

    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
// Serve stable /css, /js, /lib and /uploads URLs directly from wwwroot.
// This must run before endpoint-based static assets so a development rebuild
// cannot leave an open browser tab pointing at an obsolete fingerprint route.
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapGet("/api/payments/onepay/ipn", async (HttpRequest request, IOnePayService onePay, CancellationToken ct) =>
{
    var result = await onePay.ProcessResponseAsync(request.Query.ToDictionary(x => x.Key, x => x.Value), ct);
    return Results.Text(result.Success ? "responsecode=1&desc=confirm-success" : "responsecode=0&desc=confirm-fail", "text/plain");
}).AllowAnonymous();

app.MapGet("/api/payments/vnpay/ipn", async (HttpRequest request, VnPayService vnPay, ILogger<VnPayService> logger, CancellationToken ct) =>
{
    try
    {
        var result=await vnPay.ProcessAsync(request.Query.ToDictionary(x=>x.Key,x=>x.Value),true,ct);
        return Results.Json(new { RspCode=result.RspCode, Message=result.Message });
    }
    catch(Exception ex)
    {
        logger.LogError(ex,"VNPay IPN could not be committed.");
        return Results.Json(new { RspCode="99", Message="Retry later" });
    }
}).AllowAnonymous();

app.Run();

static string ResolveContentRoot()
{
    // Running the compiled executable directly inherits the caller's working
    // directory. Resolve the project root explicitly so /css, /js and /lib
    // never become successful-but-empty responses.
    var candidates = new[]
    {
        Environment.CurrentDirectory,
        AppContext.BaseDirectory,
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."))
    };

    foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        if (Directory.Exists(Path.Combine(candidate, "wwwroot")) &&
            Directory.Exists(Path.Combine(candidate, "Pages")))
        {
            return candidate;
        }
    }

    return Environment.CurrentDirectory;
}

static void LoadSocialLoginSettings(ConfigurationManager configuration, bool isDevelopment)
{
    var connectionString = configuration.GetConnectionString("TopstarDb");
    if (string.IsNullOrWhiteSpace(connectionString)) return;

    try
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Provider, RedirectUri, ClientId, ClientSecret FROM dbo.SocialLoginConfigurations";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var provider = reader.GetString(0);
            var redirectUri = reader.IsDBNull(1) ? null : reader.GetString(1);
            var clientId = reader.IsDBNull(2) ? null : reader.GetString(2);
            var clientSecret = reader.IsDBNull(3) ? null : reader.GetString(3);
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret)) continue;
            var facebook = provider.Equals("Facebook", StringComparison.OrdinalIgnoreCase);
            configuration[$"Authentication:{provider}:{(facebook ? "AppId" : "ClientId")}"] = clientId;
            configuration[$"Authentication:{provider}:{(facebook ? "AppSecret" : "ClientSecret")}"] = clientSecret;
            var redirectKey = $"Authentication:{provider}:RedirectUri";
            var validRedirect = Uri.TryCreate(redirectUri, UriKind.Absolute, out var parsedRedirect)
                && parsedRedirect.Scheme == Uri.UriSchemeHttps
                && (isDevelopment || !parsedRedirect.IsLoopback);
            if (validRedirect && (!isDevelopment || string.IsNullOrWhiteSpace(configuration[redirectKey])))
                configuration[redirectKey] = redirectUri;
        }
    }
    catch (SqlException)
    {
        // On a first deployment migration 137 may not exist yet; appsettings remains the fallback.
    }
}

static PathString GetCallbackPath(string? redirectUri, string fallback)
    => Uri.TryCreate(redirectUri, UriKind.Absolute, out var parsed) && !string.IsNullOrWhiteSpace(parsed.AbsolutePath)
        ? new PathString(parsed.AbsolutePath)
        : new PathString(fallback);

static string ReplaceQueryParameter(string url, string key, string value)
{
    var parsed = new UriBuilder(url);
    var parameters = QueryHelpers.ParseQuery(parsed.Query)
        .SelectMany(pair => pair.Value.Select(item => new KeyValuePair<string, string?>(pair.Key, item)))
        .Where(pair => !pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
        .Append(new KeyValuePair<string, string?>(key, value));
    parsed.Query = QueryString.Create(parameters).Value?.TrimStart('?') ?? string.Empty;
    return parsed.Uri.ToString();
}

static string ResolveOAuthRedirectUri(HttpRequest request, string? configuredRedirectUri, string callbackPath)
{
    var host = request.Host.Host;
    if (host.Equals("topstar.vn", StringComparison.OrdinalIgnoreCase)
        || host.Equals("www.topstar.vn", StringComparison.OrdinalIgnoreCase))
        return $"https://topstar.vn{callbackPath}";

    if (Uri.TryCreate(configuredRedirectUri, UriKind.Absolute, out var configured))
        return configured.ToString();

    return $"{request.Scheme}://{request.Host}{request.PathBase}{callbackPath}";
}
