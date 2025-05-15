using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RespawnApi.Application.Interfaces;
using RespawnApi.Application.Services;
using RespawnApi.Data;
using RespawnApi.DataAccess.Interfaces;
using RespawnApi.DataAccess.Repositories;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowRespawnApp", policyBuilder =>
    {
        policyBuilder.WithOrigins("http://localhost:5173") // Adresa frontend serverú
                     .AllowAnyHeader()
                     .AllowAnyMethod();
    });
});

// connection to database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersionsString = builder.Configuration["MySqlSettings:ServerVersion"] ?? "10.3.32";
var serverVersion = new MySqlServerVersion(new Version(serverVersionsString));

builder.Services.AddDbContext<RespawnDbContext>(options =>
    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)
        ));

// pridani identity pro autentizaci
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true; // vyžaduje číslice
    options.Password.RequireLowercase = true; // vyžaduje malá písmena
    options.Password.RequireUppercase = true; // vyžaduje velká písmena
    options.Password.RequireNonAlphanumeric = false; // nevyžaduje speciální znaky
    options.Password.RequiredLength = 4; // minimální délka hesla

    options.User.RequireUniqueEmail = true; // vyžaduje unikátní email
})
.AddEntityFrameworkStores<RespawnDbContext>() // přidání DbContextu
.AddDefaultTokenProviders(); // přidání výchozích poskytovatelů tokenů

// konfigurace jwt tokenů
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ??
                                  throw new InvalidOperationException("JWT Key not found configuration."));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // pro vývojové prostředí -> v produkci by mělo být true
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero // Odebere výchozí 5minutovou toleranci
    };
});

// Dependency Injection
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();

app.UseCors("AllowRespawnApp"); // použití CORS policy

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
