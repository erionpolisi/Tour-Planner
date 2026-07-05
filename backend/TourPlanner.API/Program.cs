using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TourPlanner.API.Middleware;
using TourPlanner.BusinessLayer.Services;
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

builder.Services.AddScoped<ITourService, TourService>();
builder.Services.AddScoped<ITourLogService, TourLogService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

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

// Translates business-layer exceptions to clean HTTP responses.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(AngularDevCors);
}

app.UseHttpsRedirection();
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

