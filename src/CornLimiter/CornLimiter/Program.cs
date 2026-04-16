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
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();

ConfigureValidators(builder);
ConfigureLogging(builder);
ConfigureOptions(builder);
ConfigureDbContext(builder);
ConfigureRepositories(builder);
ConfigureUseCases(builder);
ConfigureSwagger(builder);
ConfigureHealthChecks(builder);
ConfigureMappingProfiles(builder);
ConfigureVersioning(builder);
ConfigureJwtAuthentication(builder);

var exceptionlessSection = builder.Configuration.GetSection("Exceptionless");
var exceptionlessOptions = exceptionlessSection.Get<ExceptionlessOptions>();
ConfigureExceptionsService(builder, exceptionlessOptions!);


var app = builder.Build();

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

static void ConfigureValidators(WebApplicationBuilder builder)
{
     builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<SellOneCommandValidator>();
}

static void ConfigureOptions(WebApplicationBuilder builder)
{
    builder.Services.Configure<CornLimiterOptions>(builder.Configuration.GetSection("CornLimiter"));
    builder.Services.Configure<ExceptionlessOptions>(builder.Configuration.GetSection("Exceptionless"));
    builder.Services.Configure<JwtTokenOptions>(builder.Configuration.GetSection("Jwt"));
}

static void ConfigureDbContext(WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        builder.Services.AddDbContext<MySqlDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
    }

    builder.Services.AddScoped<IUnitOfWork>(c => c.GetRequiredService<MySqlDbContext>());
}

static void ConfigureExceptionsService(WebApplicationBuilder builder, ExceptionlessOptions exceptionlessOptions)
{
    if (!string.IsNullOrWhiteSpace(exceptionlessOptions?.ApiKey))
    {
        builder.Services.AddExceptionless(exceptionlessOptions.ApiKey);
        builder.Services.AddHttpContextAccessor();
    }
}

static void ConfigureSwagger(WebApplicationBuilder builder)
{
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
}

static void ConfigureHealthChecks(WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services
        .AddHealthChecks()
        .AddMySql(connectionString!);
}

static void ConfigureMappingProfiles(WebApplicationBuilder builder)
{
    builder.Services.AddAutoMapper(cfg => { }, typeof(SaleMapProfile));
}

static void ConfigureVersioning(WebApplicationBuilder builder)
{
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
}

static void ConfigureJwtAuthentication(WebApplicationBuilder builder)
{
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
}

static void ConfigureLogging(WebApplicationBuilder builder)
{
    builder.Services.AddLogging(b =>
    {
        b.AddExceptionless();
    });
}

static void ConfigureRepositories(WebApplicationBuilder builder)
{
    builder.Services.AddScoped<ISaleRepository, SaleRepository>();
}

static void ConfigureUseCases(WebApplicationBuilder builder)
{
    builder.Services.AddScoped<SellOneUseCase>();
}