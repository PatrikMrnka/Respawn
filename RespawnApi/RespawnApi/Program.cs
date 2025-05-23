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
    options.Password.RequireUppercase = false; // nevyžaduje velká písmena
    options.Password.RequireNonAlphanumeric = false; // nevyžaduje speciální znaky
    options.Password.RequiredLength = 5; // minimální délka hesla

    options.User.RequireUniqueEmail = true; // vyžaduje unikátní email
})
.AddEntityFrameworkStores<RespawnDbContext>() // přidání DbContextu
.AddDefaultTokenProviders(); // přidání výchozích poskytovatelů tokenů

// role
builder.Services.AddIdentityCore<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<RespawnDbContext>();

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

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await SeedRolesAndAdminAsync(userManager, roleManager, logger, services);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Chyba pri seedovani databaze.");
    }
}

// Metoda pro seedovani
async Task SeedRolesAndAdminAsync(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<Program> logger, IServiceProvider services)
{
    string[] roleNames = { RespawnApi.Domain.Enums.UserRoles.Administrator, RespawnApi.Domain.Enums.UserRoles.Spravce, RespawnApi.Domain.Enums.UserRoles.Uzivatel };
    foreach (var roleName in roleNames)
    {
        var roleExist = await roleManager.RoleExistsAsync(roleName);
        if (!roleExist)
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
            logger.LogInformation("Role '{RoleName}' byla vytvorena.", roleName);
        }
    }

    var adminUser = await userManager.FindByNameAsync("patricek");
    if (adminUser == null)
    {
        var newAdmin = new IdentityUser
        {
            UserName = "patricek",
            Email = "patrik.mrnka12@gmail.com",
            EmailConfirmed = true
        };
        var createAdminResult = await userManager.CreateAsync(newAdmin, "a1234");
        if (createAdminResult.Succeeded)
        {
            await userManager.AddToRoleAsync(newAdmin, RespawnApi.Domain.Enums.UserRoles.Administrator);
            logger.LogInformation("Uzivatel 'patricek' byl vytvoren a prirazen do role Administrator.");

            // Vytvoření UserProfile pro admina
            var userProfileRepository = services.GetRequiredService<RespawnApi.DataAccess.Interfaces.IUserProfileRepository>();
            var adminProfile = new RespawnApi.Domain.Entities.UserProfile
            {
                UserId = newAdmin.Id,
                Nickname = newAdmin.UserName,
                AvatarUrl = null // Nebo výchozí URL
            };
            await userProfileRepository.AddAsync(adminProfile);
            logger.LogInformation("UserProfile pro 'patricek' byl vytvoren.");
        }
        else
        {
            foreach (var error in createAdminResult.Errors)
            {
                logger.LogError("Chyba pri vytvareni uzivatele 'patricek': {ErrorDescription}", error.Description);
            }
        }
    }
    else
    {
        logger.LogInformation("Uzivatel 'patricek' jiz existuje.");
        // Ujistete se, ze existujici patricek je admin
        if (!await userManager.IsInRoleAsync(adminUser, RespawnApi.Domain.Enums.UserRoles.Administrator))
        {
            await userManager.AddToRoleAsync(adminUser, RespawnApi.Domain.Enums.UserRoles.Administrator);
            logger.LogInformation("Uzivatel 'patricek' byl prirazen do role Administrator.");
        }
    }
}



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


//app.UseHttpsRedirection();

app.UseCors("AllowRespawnApp"); // použití CORS policy

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
