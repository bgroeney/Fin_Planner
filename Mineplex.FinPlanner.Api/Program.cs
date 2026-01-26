using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Services;
using Mineplex.FinPlanner.Api.Services.PriceProviders;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals;
    });
builder.Services.AddOpenApi();

// Configure CORS - supports both local development and production origins
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:5174" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IDemoDataService, DemoDataService>();
builder.Services.AddScoped<INetwealthImportService, NetwealthImportService>();
builder.Services.AddScoped<Mineplex.FinPlanner.Api.Services.Import.INetwealthCsvParser, Mineplex.FinPlanner.Api.Services.Import.NetwealthCsvParser>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IAIService, GeminiAIService>();
builder.Services.AddScoped<IStressTestEngine, MonteCarloService>();
builder.Services.AddScoped<ITaxOptimizationService, TaxOptimizationService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();
builder.Services.AddScoped<ICommercialPropertyService, CommercialPropertyService>();
builder.Services.AddScoped<IMarketDataService, MarketDataService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ITaxDistributionService, TaxDistributionService>();
builder.Services.AddScoped<IRebalancingService, RebalancingService>();
builder.Services.AddHttpClient();

// Register Price Providers
builder.Services.AddScoped<IPriceSourceProvider, YahooFinanceProvider>();
builder.Services.AddScoped<IPriceSourceProvider, AlphaVantageProvider>();
builder.Services.AddScoped<IPriceSourceProvider, PolygonProvider>();
builder.Services.AddScoped<IPriceSourceProvider, MorningstarAuProvider>();
builder.Services.AddScoped<IPriceSourceProvider, ImportedPriceProvider>();

// Register Price System Manager
builder.Services.AddScoped<PriceSourceManager>();

// Register Price Update Service (on-demand price updates only - no background service)
builder.Services.AddScoped<IPriceUpdateService, PriceUpdateService>();


// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "super_secret_key_please_change_in_production_environment_1234567890";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "FinPlanner",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "FinPlannerUser",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddDbContext<FinPlannerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint for Cloud Run
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
