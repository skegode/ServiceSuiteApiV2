using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Enable detailed Identity logging for debugging
Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

// ── 1. Swagger Configuration (Compatible with Microsoft.OpenApi 1.6.x) ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Use fully qualified names to resolve reference/namespace issues
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ServiceSuite API V2",
        Version = "v1",
        Description = "Loan Management and Write-off API"
    });

    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer token **_only_**",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    options.AddSecurityDefinition("Bearer", securityScheme);

    var securityRequirement = new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    };

    options.AddSecurityRequirement(securityRequirement);
});

// ── 2. JWT Authentication Setup ───────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "YourDefaultFallbackSecretKey123!";
var keyBytes = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[JWT] Auth failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("[JWT] Token validated successfully.");
                return Task.CompletedTask;
            }
        };
    });

// ── 3. CORS Policy ───────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// ── 4. Dependency Injection & Controllers ─────────────────────
builder.Services.AddControllers();
builder.Services.AddAuthorization();

// Using full namespaces for services to avoid folder/namespace confusion
builder.Services.AddScoped<ServiceSuiteApiV2.Controllers.ILoanService, ServiceSuiteApiV2.LoanService>();
builder.Services.AddScoped<ServiceSuiteApiV2.IAuthService, ServiceSuiteApiV2.AuthService>();

var app = builder.Build();

// ── 5. Middleware Pipeline ────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ServiceSuite API V2");
    });
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();