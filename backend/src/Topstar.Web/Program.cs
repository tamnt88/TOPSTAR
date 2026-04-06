using Microsoft.AspNetCore.Authentication.Cookies;
using Topstar.Application;
using Topstar.Infrastructure;
using Topstar.Web.Services.Email;
using Topstar.Web.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
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
    })
    .AddCookie("External", options =>
    {
        options.Cookie.Name = "topstar.external";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    });

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication().AddGoogle("Google", options =>
    {
        options.SignInScheme = "External";
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
    });
}

var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
if (!string.IsNullOrWhiteSpace(facebookAppId) && !string.IsNullOrWhiteSpace(facebookAppSecret))
{
    builder.Services.AddAuthentication().AddFacebook("Facebook", options =>
    {
        options.SignInScheme = "External";
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
        options.CallbackPath = "/signin-facebook";
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
builder.Services.AddTopstarApplication();
builder.Services.AddTopstarInfrastructure(builder.Configuration);
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
