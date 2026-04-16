using Asp.Versioning;
using CornLimiter.Application.UseCases;
using CornLimiter.Application.Validators;
using CornLimiter.Configuration;
using CornLimiter.Domain;
using CornLimiter.Domain.MapProfiles;
using CornLimiter.Infrastructure.Data;
using CornLimiter.Infrastructure.Data.Repositories;
using CornLimiter.Presentation.Middleware;
using Exceptionless;
using FluentValidation;
using FluentValidation.AspNetCore;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

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

// Bind options
builder.Services.Configure<CornLimiterOptions>(builder.Configuration.GetSection("CornLimiter"));
builder.Services.Configure<ExceptionlessOptions>(builder.Configuration.GetSection("Exceptionless"));
builder.Services.Configure<JwtTokenOptions>(builder.Configuration.GetSection("Jwt"));


// Configure MySQL DbContext (usa ConnectionStrings:DefaultConnection)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<MySqlDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
}


// Registrar repositorios y casos de uso (inyección por interfaz)
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<SellOneUseCase>();
builder.Services.AddScoped<IUnitOfWork>(c => c.GetRequiredService<MySqlDbContext>());

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
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token."
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services
    .AddHealthChecks()
    .AddMySql(connectionString!);


builder.Services.AddAutoMapper(cfg => { }, typeof(SaleMapProfile));

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc() // This is needed for controllers
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

// Configurar autenticación JWT 
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtTokenOptions = jwtSection.Get<JwtTokenOptions>();

if (jwtTokenOptions != null)
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtTokenOptions.Issuer,
            ValidAudience = jwtTokenOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtTokenOptions.Key))
        };
    });
}



var app = builder.Build();


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


// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionsMiddleware>();
app.UseMiddleware<LoggerMiddleware>();
app.UseMiddleware<MetricsMiddleware>();


// Registrar middleware que reporta excepciones a Exceptionless (opcional si AddExceptionless fue registrado)
if (!string.IsNullOrWhiteSpace(exceptionlessOptions?.ApiKey))
{
    app.UseExceptionless();
}

app.MapHealthChecks("/healthcheck", new HealthCheckOptions()
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
