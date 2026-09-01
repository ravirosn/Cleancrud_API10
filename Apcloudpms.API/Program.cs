using Apcloudpms.API.Middleware;
using Apcloudpms.API.Authorization;
using Apcloudpms.API.Services;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Application.Mappings;
using Apcloudpms.Application.Validators;
using Apcloudpms.Infrastructure.Data;
using Apcloudpms.Infrastructure.Repositories;
using Apcloudpms.Infrastructure.Services;
using Apcloudpms.Infrastructure.Options;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtKey = jwtSection[nameof(JwtOptions.Key)];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must be configured with at least 32 bytes.");

builder.Services.AddOptions<JwtOptions>()
    .Bind(jwtSection)
    .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer), "Jwt:Issuer is required.")
    .Validate(x => !string.IsNullOrWhiteSpace(x.Audience), "Jwt:Audience is required.")
    .Validate(x => x.AccessTokenMinutes is >= 1 and <= 360,
        "Jwt:AccessTokenMinutes must be between 1 and 360.")
    .Validate(x => x.RefreshTokenDays >= 1, "Jwt:RefreshTokenDays must be positive.")
    .Validate(x => x.RefreshTokenAbsoluteDays >= x.RefreshTokenDays,
        "Jwt:RefreshTokenAbsoluteDays must be at least RefreshTokenDays.")
    .ValidateOnStart();

builder.Services.AddControllers();
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
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<StudentDtoValidator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    const string bearerScheme = "Bearer";

    options.AddSecurityDefinition(bearerScheme, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter an access token. Swagger adds the 'Bearer' prefix automatically. " +
                      "Both Apcloudpms JWTs and Microsoft Entra access tokens are supported."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(bearerScheme, document)] = []
    });
});
builder.Services.AddAutoMapper(cfg => { }, typeof(StudentProfile).Assembly);
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAuditContext, HttpAuditContext>();

builder.Services.AddDbContextPool<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(
        maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10),
        errorNumbersToAdd: null)), poolSize: 128);

builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccessControlService, AccessControlService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IEmailQueueService, EmailQueueService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IModuleAccessService, ModuleAccessService>();
builder.Services.AddScoped<IUserThemeSettingService, UserThemeSettingService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IProfileImageStorage, ProfileImageStorage>();
builder.Services.AddScoped<IListItemService, ListItemService>();
builder.Services.AddScoped<IRoleModuleMenuManagementService, RoleModuleMenuManagementService>();
builder.Services.AddScoped<IPermitApplicationService, PermitApplicationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IRiskAssessmentService, RiskAssessmentService>();
builder.Services.AddScoped<IApprovalWorkflowService, ApprovalWorkflowService>();
builder.Services.AddScoped<IWorkflowSetupService, WorkflowSetupService>();
builder.Services.AddSingleton<IApprovalNotificationQueue, ApprovalNotificationQueue>();
builder.Services.AddHostedService<ApprovalNotificationWorker>();
builder.Services.AddHostedService<EmailQueueWorker>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IEntraUserService, EntraUserService>();
builder.Services.AddScoped<IPowerBiService, PowerBiService>();
builder.Services.AddOptions<ProfileImageStorageOptions>()
    .Bind(builder.Configuration.GetSection(ProfileImageStorageOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.LocalFolder) &&
                         options.MaximumUploadBytes >= options.MaximumSavedBytes &&
                         options.MaximumDimension is >= 320 and <= 4096 &&
                         options.MaximumSavedBytes > 0 &&
                         options.MaximumSavedBytes <= 2 * 1024 * 1024,
        "LocalFolder, upload limits, saved limits, or image dimensions are invalid.")
    .Validate(options => !options.UseAzure ||
                         (!string.IsNullOrWhiteSpace(options.Azure.ConnectionString) &&
                          !string.IsNullOrWhiteSpace(options.Azure.ContainerName)),
        "Azure connection settings are required when profile image Azure storage is enabled.")
    .ValidateOnStart();
builder.Services.AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.SectionName))
    .Validate(options => options.PollSeconds is >= 2 and <= 300 && options.BatchSize is >= 1 and <= 100 &&
        options.LeaseMinutes is >= 1 and <= 60, "Email queue processing settings are invalid.")
    .Validate(options => !options.Enabled || (!string.IsNullOrWhiteSpace(options.Host) &&
        options.Port is >= 1 and <= 65535 && !string.IsNullOrWhiteSpace(options.FromAddress)),
        "SMTP host, port, and from address are required when email is enabled.")
    .ValidateOnStart();
builder.Services.AddOptions<PasswordResetOptions>()
    .Bind(builder.Configuration.GetSection(PasswordResetOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.ResetPageUrl, UriKind.Absolute, out _) &&
        options.TokenLifetimeMinutes is >= 10 and <= 1440, "Password reset URL or token lifetime is invalid.")
    .ValidateOnStart();
builder.Services.AddHttpClient("PowerBi", client =>
    client.BaseAddress = new Uri("https://api.powerbi.com/v1.0/myorg/"));
builder.Services.AddOptions<EntraProvisioningOptions>()
    .Bind(builder.Configuration.GetSection(EntraProvisioningOptions.SectionName));
builder.Services.AddOptions<PowerBiOptions>()
    .Bind(builder.Configuration.GetSection(PowerBiOptions.SectionName))
    .PostConfigure(options =>
    {
        if (string.IsNullOrWhiteSpace(options.TenantId))
            options.TenantId = builder.Configuration["AzureAd:TenantId"] ?? string.Empty;
    });

const string smartBearer = "SmartBearer";
const string localBearer = "LocalBearer";
const string entraBearer = "EntraBearer";
var authentication = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = smartBearer;
    options.DefaultChallengeScheme = smartBearer;
})
.AddPolicyScheme(smartBearer, smartBearer, options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return entraBearer;
        try
        {
            var token = new JsonWebTokenHandler().ReadJsonWebToken(authorization[7..].Trim());
            return token.Issuer.Contains("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase) ||
                   token.Issuer.Contains("sts.windows.net", StringComparison.OrdinalIgnoreCase)
                ? entraBearer
                : localBearer;
        }
        catch
        {
            return entraBearer;
        }
    };
})
.AddJwtBearer(localBearer, options =>
{
    options.MapInboundClaims = false;
    options.SaveToken = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection[nameof(JwtOptions.Issuer)],
        ValidAudience = jwtSection[nameof(JwtOptions.Audience)],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };
});
authentication.AddMicrosoftIdentityWebApi(
    builder.Configuration.GetSection("AzureAd"), entraBearer);

var apiScope = builder.Configuration["AzureAd:Scopes"] ?? "access_as_user";
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(smartBearer)
        .RequireAuthenticatedUser()
        .AddRequirements(new ApiScopeRequirement(apiScope))
        .Build();
});
builder.Services.AddSingleton<IAuthorizationPolicyProvider, ModulePolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, ModuleAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ApiScopeAuthorizationHandler>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetSlidingWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueLimit = 0
        }));
});

var app = builder.Build();

// Avoid compressing token-bearing authentication responses. Other API clients
// can negotiate Brotli/Gzip, while the Web HttpClient decompresses automatically.
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api/auth"),
    branch => branch.UseResponseCompression());

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<EntraUserMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();
