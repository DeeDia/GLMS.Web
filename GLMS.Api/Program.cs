using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using GLMS.Api.Data;

var builder = WebApplication.CreateBuilder(args);

//Database
builder.Services.AddDbContext<GlmsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

//HttpClient (for currency API)
builder.Services.AddHttpClient();

//Controllers
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Prevent circular reference errors when serialising
        opts.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

//JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
                ?? "DefaultSecretKeyForDevelopment123!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "GLMS.Api",
            ValidAudience = "GLMS.Web",
            IssuerSigningKey = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtSecret)),
        };
    });

builder.Services.AddAuthorization();

//Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GLMS API",
        Version = "v1",
        Description = "Global Logistics Management System — TechMove Logistics",
    });

    // Allow Swagger UI to send JWT token in the Authorization header
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter: Bearer {your token}",
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
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

//CORS (allows the MVC app to call this API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvcApp", policy =>
        policy.WithOrigins(
                  "http://localhost:29385",
                  "https://localhost:44307",
                  "http://localhost:5043",
                  "https://localhost:7282",
                  "http://glms-frontend-web")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// This runs migrations automatically when the container starts
// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<GlmsDbContext>();

    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        // For in-memory test database just ensure it is created
        db.Database.EnsureCreated();
    }
}

// This enables Swagger in all environments so it is visible when running in Docker
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GLMS API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowMvcApp");
//app.UseHttpsRedirection();
app.UseAuthentication();   // ← must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }