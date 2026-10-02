using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RaigonArts.Api.Common;
using RaigonArts.Api.Data;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Middleware;
using RaigonArts.Api.Models;
using RaigonArts.Api.Services;

// Configure UTF-8 console encoding for emoji and special characters support
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);


// 1. Database Context
var useInMemory = builder.Configuration.GetValue<bool>("ConnectionStrings:UseInMemoryDatabase");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    if (builder.Environment.IsDevelopment())
    {
        connectionString = "Host=localhost;Port=5432;Database=raigonarts_db;Username=raigon_user;Password=RaigonDb@2026";
    }
    else if (!useInMemory)
    {
        throw new InvalidOperationException("Production database connection string is not configured. Please set the 'ConnectionStrings__DefaultConnection' environment variable.");
    }
}

builder.Services.AddDbContext<RaigonDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("RaigonArtsDb");
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

// 2. WhatsApp & AI Assistant Settings & Clients
builder.Services.Configure<WhatsAppSettings>(builder.Configuration.GetSection(WhatsAppSettings.SectionName));
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>();

builder.Services.Configure<AiSettings>(builder.Configuration.GetSection(AiSettings.SectionName));
builder.Services.AddHttpClient("AiAssistantClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

// 3. Register Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IAiToolsService, AiToolsService>();
builder.Services.AddSingleton<IAiSessionStore, AiSessionStore>();
builder.Services.AddScoped<IAiSupportService, AiSupportService>();

// 3. Controllers & Custom Model Validation
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .Select(e => new ApiErrorItem
            {
                Field = JsonNamingPolicy.CamelCase.ConvertName(e.Key),
                Message = e.Value!.Errors.First().ErrorMessage
            })
            .ToList();

        var errorResponse = ApiErrorResponse.Create(
            400,
            "BAD_REQUEST",
            "Validation failed on input fields.",
            errors
        );

        return new BadRequestObjectResult(errorResponse);
    };
});

// 4. JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (builder.Environment.IsDevelopment())
    {
        jwtKey = "RaigonArts_SuperSecretWorkshopSigningKey_2026_SecureJwtAuthToken!";
    }
    else
    {
        throw new InvalidOperationException("JWT Signing Key ('Jwt:Key') is not configured for production. Please set the 'Jwt__Key' environment variable.");
    }
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "RaigonArtsApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "RaigonArtsApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 5. CORS Configuration (Allows Angular dev server & configurable production origins)
var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var defaultOrigins = new[]
{
    "http://localhost:4200",
    "http://127.0.0.1:4200",
    "http://localhost:3000",
    "http://localhost:60469"
};
var allowedOrigins = defaultOrigins
    .Union(configuredOrigins)
    .Where(o => !string.IsNullOrWhiteSpace(o))
    .Select(o => o.TrimEnd('/'))
    .Distinct()
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowRaigonClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 6. Swagger / OpenAPI Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Raigon Arts REST API",
        Version = "v2.4.0",
        Description = "Custom Photo Framing & Customer Management System API Specification"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 7. Reverse Proxy & Forwarded Headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// 8. Initialize and Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<RaigonDbContext>();
        await DbInitializer.InitializeAsync(db);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// 9. Middleware Pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

// Enable Swagger UI in Development only
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Raigon Arts API v2.4.0");
        c.RoutePrefix = "swagger";
    });
}

// HTTPS Redirection in Production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Static Files & Uploads
var webRoot = app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
if (!Directory.Exists(webRoot)) Directory.CreateDirectory(webRoot);

var uploadsPath = Path.Combine(webRoot, "uploads");
if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(); // Serves wwwroot
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseRouting();

app.UseCors("AllowRaigonClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

