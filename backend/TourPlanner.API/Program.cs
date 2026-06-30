using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TourPlanner.API.Middleware;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.DataAccessLayer;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.Domain;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddDbContext<TourPlannerDbContext>(opt => opt.UseNpgsql(connectionString));

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

