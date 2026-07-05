using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TourPlanner.API.Middleware;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.BusinessLayer.Services.Auth;
using TourPlanner.BusinessLayer.Services.Routing;
using TourPlanner.DataAccessLayer;
using TourPlanner.DataAccessLayer.Interceptors;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.Domain;

// --- Serilog bootstrap logger -------------------------------------------------
// A minimal logger for startup errors before the host is built. Replaced below
// with the fully-configured pipeline read from appsettings.json.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{

var builder = WebApplication.CreateBuilder(args);

// Route all Microsoft.Extensions.Logging output through Serilog.
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// --- Controllers + JSON config -----------------------------------------------
// Serialize enums (if any escape from DTOs) as strings, not ints.
// Ignore null properties to keep responses small.
builder.Services
    .AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opt.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddOpenApi();

// --- Database ----------------------------------------------------------------
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "ConnectionString 'DefaultConnection' is missing. Set it via User Secrets or appsettings.");

// The audit interceptor logs every INSERT / UPDATE / DELETE that flows through EF.
builder.Services.AddSingleton<AuditSaveChangesInterceptor>();
builder.Services.AddDbContext<TourPlannerDbContext>((sp, opt) => opt
    .UseNpgsql(connectionString)
    .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

// --- Dependency Injection: repositories, services, auth helpers --------------
builder.Services.AddScoped<ITourRepository, TourRepository>();
builder.Services.AddScoped<ITourLogRepository, TourLogRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

builder.Services.AddScoped<ITourService, TourService>();
builder.Services.AddScoped<ITourLogService, TourLogService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthSessionService, AuthSessionService>();
builder.Services.AddSingleton<IPasswordPolicy, DefaultPasswordPolicy>();

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// NIST SP 800-63B: password verification must be slow. Pin PBKDF2 to the
// Identity V3 format (HMAC-SHA-256) and raise the iteration count above the
// ASP.NET default (100k) to the OWASP 2023 recommendation.
builder.Services.Configure<PasswordHasherOptions>(opt =>
{
    opt.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
    opt.IterationCount = 210_000;
});

// The session service records the client IP so operators can trace refresh-token abuse.
builder.Services.AddHttpContextAccessor();

// --- JWT authentication -------------------------------------------------------
// Access-token issuer + validation. The signing key is a secret and comes from
// user-secrets (Development) or the process environment (Production).
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

var signingKeyBytes = string.IsNullOrWhiteSpace(jwtOptions.SigningKey)
    ? Array.Empty<byte>()
    : TryFromBase64(jwtOptions.SigningKey) ?? Encoding.UTF8.GetBytes(jwtOptions.SigningKey);

if (signingKeyBytes.Length < 32)
{
    // Fail fast — the app can't safely issue tokens without a strong key.
    throw new InvalidOperationException(
        "Jwt:SigningKey must decode to ≥ 32 bytes. Set it via user-secrets: " +
        "`dotnet user-secrets set \"Jwt:SigningKey\" \"<base64-256-bit-key>\"`.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep the raw JWT claim names (sub, jti, ...)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),
            ClockSkew = TimeSpan.Zero, // NIST: no leeway on token expiry
        };
    });

builder.Services.AddAuthorization(opts =>
{
    // Default: every endpoint requires an authenticated user. Public endpoints
    // must opt out with [AllowAnonymous].
    opts.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// --- Rate limiting -----------------------------------------------------------
// Per-IP fixed-window on the auth endpoints. Blunts credential-stuffing and
// enumeration attempts without punishing legitimate users.
builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // 5 requests per IP per minute on /api/auth/login and /api/auth/register.
    opt.AddPolicy("auth", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
});

// Helper: try base64 first, fall back to plain UTF-8. Local method for Program.cs.
static byte[]? TryFromBase64(string s)
{
    try { return Convert.FromBase64String(s); }
    catch (FormatException) { return null; }
}

// --- Routing / geocoding proxy (Nominatim + OpenRouteService) ---------------
// Bind config from the "Routing" section — the ORS API key MUST come from
// user-secrets, never from committed appsettings.
builder.Services.Configure<RoutingOptions>(
    builder.Configuration.GetSection(RoutingOptions.SectionName));

// Named typed client — gets a scoped HttpClient with sane defaults.
builder.Services.AddHttpClient<IRoutingService, RoutingService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});

// --- CORS: let the Angular dev server (localhost:4200) call this API ---------
const string AngularDevCors = "AngularDev";
builder.Services.AddCors(opt =>
{
    opt.AddPolicy(AngularDevCors, p => p
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// --- Middleware pipeline -----------------------------------------------------
// Request logging is the OUTERMOST middleware so it captures the final response
// status (after ExceptionHandlingMiddleware has translated e.g. NotFoundException
// into a proper 404).
app.UseSerilogRequestLogging();

// Security headers on every response (belt-and-braces before anything else runs).
app.UseMiddleware<SecurityHeadersMiddleware>();

// Translates business-layer exceptions to clean HTTP responses.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.UseCors(AngularDevCors);
}
else
{
    // HSTS: force HTTPS for 1 year in production. Only enabled outside dev so
    // localhost testing on http://localhost:5102 keeps working.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

