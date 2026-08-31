using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Identity.Web;
using Apcloud.Web.Infrastructure;
using Apcloud.Web.Services;
using Apcloud.Web.Services.Authentication;
using System.IO.Compression;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Fastest);
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "__Host-Apcloud.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.Path = "/";
    options.IdleTimeout = TimeSpan.FromHours(8);
});
builder.Services.AddDataProtection().SetApplicationName("Apcloud.Web");
builder.Services.AddSingleton<ITicketStore, DistributedCacheTicketStore>();

builder.Services
    .AddOptions<ApiOptions>()
    .Bind(builder.Configuration.GetSection(ApiOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps,
        "Api:BaseUrl must be an absolute HTTPS URL.")
    .ValidateOnStart();

builder.Services
    .AddOptions<MicrosoftEntraOptions>()
    .Bind(builder.Configuration.GetSection(MicrosoftEntraOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => !options.Enabled || options.IsConfigured,
        "When Microsoft Entra SSO is enabled, TenantId, ClientId, ClientSecret, and at least one ApiScope are required.")
    .ValidateOnStart();

builder.Services
    .AddOptions<BffOptions>()
    .Bind(builder.Configuration.GetSection(BffOptions.SectionName))
    .Validate(
        options => options.AllowedPathPrefixes.Length > 0 && options.AllowedMethods.Length > 0,
        "Bff must define at least one allowed API path prefix and HTTP method.")
    .ValidateOnStart();

static void ConfigureApiClient(IServiceProvider services, HttpClient client)
{
    var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + '/');
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
}

builder.Services
    .AddHttpClient<IAuthApiClient, AuthApiClient>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AutomaticDecompression = DecompressionMethods.Brotli |
                                 DecompressionMethods.GZip |
                                 DecompressionMethods.Deflate
    });
builder.Services.AddTransient<ApiBearerTokenHandler>();
builder.Services
    .AddHttpClient<ApcloudApiClient>(ConfigureApiClient)
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AutomaticDecompression = DecompressionMethods.Brotli |
                                 DecompressionMethods.GZip |
                                 DecompressionMethods.Deflate
    })
    .AddHttpMessageHandler<ApiBearerTokenHandler>();

var entraConfiguration = builder.Configuration
    .GetSection(MicrosoftEntraOptions.SectionName)
    .Get<MicrosoftEntraOptions>() ?? new MicrosoftEntraOptions();

var authentication = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = AuthenticationSchemeNames.ApplicationCookie;
    options.DefaultChallengeScheme = AuthenticationSchemeNames.ApplicationCookie;
    options.DefaultSignInScheme = AuthenticationSchemeNames.ApplicationCookie;
});

if (entraConfiguration.Enabled)
{
    authentication
        .AddMicrosoftIdentityWebApp(
            builder.Configuration.GetSection(MicrosoftEntraOptions.SectionName),
            openIdConnectScheme: AuthenticationSchemeNames.MicrosoftEntra,
            cookieScheme: AuthenticationSchemeNames.ApplicationCookie,
            displayName: "Microsoft Entra ID")
        .EnableTokenAcquisitionToCallDownstreamApi(entraConfiguration.ApiScopes)
        .AddDistributedTokenCaches();

    builder.Services
        .AddOptions<OpenIdConnectOptions>(AuthenticationSchemeNames.MicrosoftEntra)
        .PostConfigure(options =>
        {
            var previousHandler = options.Events.OnTokenValidated;
            options.Events.OnTokenValidated = async context =>
            {
                if (previousHandler is not null)
                {
                    await previousHandler(context);
                }

                if (context.Principal?.Identity is System.Security.Claims.ClaimsIdentity identity &&
                    !identity.HasClaim(claim => claim.Type == "authentication_method"))
                {
                    identity.AddClaim(new System.Security.Claims.Claim(
                        "authentication_method",
                        Apcloud.Contracts.Enums.AuthenticationMethod.MicrosoftEntraId.ToString()));
                }
            };
        });
}
else
{
    authentication.AddCookie(AuthenticationSchemeNames.ApplicationCookie);
}

builder.Services
    .AddOptions<CookieAuthenticationOptions>(AuthenticationSchemeNames.ApplicationCookie)
    .Configure<ITicketStore>((options, ticketStore) =>
    {
        options.LoginPath = "/Authentication/Account/Login";
        options.AccessDeniedPath = "/Authentication/Account/Login";
        options.Cookie.Name = "__Host-Apcloud.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.Path = "/";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
        options.SessionStore = ticketStore;
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/bff"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/bff"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "__Host-Apcloud.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.Path = "/";
    options.HeaderName = "X-CSRF-TOKEN";
});

var app = builder.Build();

// Authentication responses can contain tokens and other secrets. Keep them out
// of HTTPS response compression while compressing normal MVC and BFF responses.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/Authentication/Account"),
    branch => branch.UseResponseCompression());

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/PermitManagement/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "root",
    pattern: "",
    defaults: new { area = "Authentication", controller = "Account", action = "Login" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
