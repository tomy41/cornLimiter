using CornLimiter.Application.UseCases;
using CornLimiter.Application.Validators;
using CornLimiter.Configuration;
using CornLimiter.Domain;
using CornLimiter.Infrastructure.Data;
using CornLimiter.Infrastructure.Data.Repositories;
using CornLimiter.Middleware;
using FluentValidation;
using FluentValidation.AspNetCore;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Exceptionless;
using Microsoft.OpenApi;
using CornLimiter.Domain.MapProfiles;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<SellOneCommandValidator>();

// Caching services
builder.Services.AddMemoryCache();

// Adds logging services and configures Exceptionless as a logging provider
builder.Services.AddLogging(b => {
    b.AddExceptionless();
});

// Bind CornLimiter options
builder.Services.Configure<CornLimiterOptions>(builder.Configuration.GetSection("CornLimiter"));

// Bind Exceptionless options (disponible vía IOptions<ExceptionlessOptions>)
builder.Services.Configure<ExceptionlessOptions>(builder.Configuration.GetSection("Exceptionless"));

// Configure MySQL DbContext (usa ConnectionStrings:DefaultConnection)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<MySqlDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
}

builder.Services.AddScoped<IUnitOfWork>(c => c.GetRequiredService<MySqlDbContext>());

// Registrar repositorios y casos de uso (inyección por interfaz)
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<SellOneUseCase>();

// Leer las opciones bindeadas para registrar Exceptionless (la sección ya está vinculada a IOptions)
var exceptionlessSection = builder.Configuration.GetSection("Exceptionless");
var exceptionlessOptions = exceptionlessSection.Get<ExceptionlessOptions>();
if (!string.IsNullOrWhiteSpace(exceptionlessOptions?.ApiKey))
{
    builder.Services.AddExceptionless(exceptionlessOptions.ApiKey);
    builder.Services.AddHttpContextAccessor();
}

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CornLimiter API",
        Version = "v1",
        Description = "Documentación OpenAPI para CornLimiter"
    });
});

builder.Services
    .AddHealthChecks()
    .AddMySql(connectionString!);

builder.Services
    .AddHealthChecksUI()
    .AddInMemoryStorage();


builder.Services.AddAutoMapper(cfg => { }, typeof(SaleMapProfile));

var app = builder.Build();

// Configure the HTTP request pipeline.

// Habilitar Swagger y SwaggerUI en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CornLimiter API v1");
        c.RoutePrefix = string.Empty;
    });
}

// Registrar middlewares
app.UseMiddleware<LoggerMiddleware>();
app.UseMiddleware<ExceptionlessMiddleware>();
    

// Registrar middleware que reporta excepciones a Exceptionless (opcional si AddExceptionless fue registrado)
if (!string.IsNullOrWhiteSpace(exceptionlessOptions?.ApiKey))
{
    app.UseExceptionless(); 
}

app.MapHealthChecks("/api/health", new HealthCheckOptions()
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseHealthChecksUI(options =>
{
    options.UIPath = "/healthcheck-ui";
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
