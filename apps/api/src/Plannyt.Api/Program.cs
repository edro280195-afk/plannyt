using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Plannyt.Api.BuildingBlocks.Configuration;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.BuildingBlocks.Http;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Access.Application;
using Plannyt.Api.Modules.Access.Authorization;
using Plannyt.Api.Modules.Access.Security;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Catalog.Application;
using Plannyt.Api.Modules.Contracts.Application;
using Plannyt.Api.Modules.Contracts.Pdf;
using Plannyt.Api.Modules.Contracts.Rendering;
using Plannyt.Api.Modules.Contracts.Security;
using Plannyt.Api.Modules.Crm.Application;
using Plannyt.Api.Modules.Crm.Domain;
using Plannyt.Api.Modules.Documents.Application;
using Plannyt.Api.Modules.Documents.Storage;
using Plannyt.Api.Modules.Events.Application;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Identity.Application;
using Plannyt.Api.Modules.Identity.Domain;
using Plannyt.Api.Modules.Identity.Security;
using Plannyt.Api.Modules.Organizations.Application;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Payments.Application;
using Plannyt.Api.Modules.Proposals.Application;
using Plannyt.Api.Modules.Proposals.Domain;
using Plannyt.Api.Modules.Proposals.Pdf;
using Plannyt.Api.Modules.Proposals.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});

builder.Services
    .AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddOptions<CorsOptions>()
    .BindConfiguration(CorsOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddOptions<DatabaseOptions>()
    .BindConfiguration(DatabaseOptions.SectionName)
    .ValidateOnStart();
builder.Services
    .AddOptions<FileStorageOptions>()
    .BindConfiguration(FileStorageOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddOptions<DemoSeedOptions>()
    .BindConfiguration(DemoSeedOptions.SectionName)
    .ValidateOnStart();
builder.Services
    .AddOptions<FrontendOptions>()
    .BindConfiguration(FrontendOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddOptions<RateLimitOptions>()
    .BindConfiguration(RateLimitOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwtOptions = builder.Configuration
    .GetRequiredSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("No se encontró la configuración JWT.");
var corsOptions = builder.Configuration
    .GetRequiredSection(CorsOptions.SectionName)
    .Get<CorsOptions>()
    ?? throw new InvalidOperationException("No se encontró la configuración CORS.");
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["correlationId"] =
            context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Plannyt API",
        Version = "v1",
        Description = "API multi-tenant para la operación y portal de Plannyt."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Access token de corta duración."
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicies.Frontend, policy =>
    {
        policy
            .WithOrigins(corsOptions.AllowedOrigin)
            .AllowAnyMethod()
            .WithHeaders("Authorization", "Content-Type", "X-Plannyt-Client")
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<OrganizationSlugGenerator>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<CookieRequestGuard>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<RefreshCookieService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<TenantAccessService>();
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<ProspectService>();
builder.Services.AddSingleton<ProspectStatusTransitionService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<ProposalService>();
builder.Services.AddSingleton<ProposalTotalsCalculator>();
builder.Services.AddSingleton<ProposalTokenService>();
builder.Services.AddSingleton<IProposalPdfGenerator, ProposalPdfGenerator>();
builder.Services.AddScoped<ContractTemplateService>();
builder.Services.AddScoped<ContractVariableValueService>();
builder.Services.AddScoped<ContractService>();
builder.Services.AddScoped<SignatureService>();
builder.Services.AddScoped<ContractingReadinessService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddSingleton<ContractTemplateRenderer>();
builder.Services.AddSingleton<SignatureTokenService>();
builder.Services.AddSingleton<IContractPdfGenerator, ContractPdfGenerator>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<EventStatusTransitionService>();
builder.Services.AddScoped<DemoDataSeeder>();
builder.Services.AddScoped<InvitationService>();
builder.Services.AddScoped<EventAccessService>();
builder.Services.AddScoped<PortalAccessService>();
builder.Services.AddScoped<PortalEventService>();
builder.Services.AddSingleton<InvitationTokenService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddSingleton<DocumentFileValidator>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Configura ConnectionStrings:DefaultConnection.");

builder.Services.AddDbContext<PlannytDbContext>(options =>
    options
        .UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention());

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Sensitive, httpContext =>
    {
        var permitLimit = httpContext.RequestServices
            .GetRequiredService<
                Microsoft.Extensions.Options.IOptions<RateLimitOptions>>()
            .Value
            .SensitivePermitLimit;
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<PlannytDbContext>("postgresql", tags: ["ready"]);

var app = builder.Build();

DemoSeedGuard.Validate(app.Environment, app.Configuration);
FileStorageGuard.Validate(app.Environment);
await DatabaseInitializer.InitializeAsync(app);
await DemoDataInitializer.InitializeAsync(app);

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicies.Frontend);
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<SessionValidationMiddleware>();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();

app.MapHealthChecks("/health/ready").AllowAnonymous();
app.MapAuthEndpoints();
app.MapOrganizationEndpoints();
app.MapClientEndpoints();
app.MapProspectEndpoints();
app.MapCatalogEndpoints();
app.MapProposalEndpoints();
app.MapContractEndpoints();
app.MapPaymentEndpoints();
app.MapEventEndpoints();
app.MapAccessEndpoints();
app.MapDocumentEndpoints();

app.MapGet("/", () => Results.Ok(new
{
    service = "Plannyt API",
    status = "ok"
})).AllowAnonymous();

app.Run();

public partial class Program;
