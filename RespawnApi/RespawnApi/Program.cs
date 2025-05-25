using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RespawnApi.Application.Interfaces;
using RespawnApi.Application.Services;
using RespawnApi.Application.Services.Factories;
using RespawnApi.Application.Services.Strategies;
using RespawnApi.Data;
using RespawnApi.DataAccess.Interfaces;
using RespawnApi.DataAccess.Repositories;
using RespawnApi.Hubs;

var builder = WebApplication.CreateBuilder(args);

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowRespawnApp", policyBuilder =>
    {
        policyBuilder.WithOrigins("http://localhost:5173") // Frontend URL
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR with credentials
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serverVersionsString =
    builder.Configuration["MySqlSettings:ServerVersion"] ?? "10.3.32"; // Default version if not specified
var serverVersion = new MySqlServerVersion(new Version(serverVersionsString));

builder.Services.AddDbContext<RespawnDbContext>(options =>
    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)
    ));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 5;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<RespawnDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddIdentityCore<IdentityUser>()
    .AddRoles<IdentityRole>() // Ensure roles are available for IdentityCore
    .AddEntityFrameworkStores<RespawnDbContext>();


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
        options.RequireHttpsMetadata = false; // Set to true in production
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero // Remove clock skew for precise expiration
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/pollHub") ||
                     path.StartsWithSegments("/presenceHub") ||
                     path.StartsWithSegments("/gameServerHub") ||
                     path.StartsWithSegments("/serverLogHub")))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// Dependency Injection
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<IUserPresenceService, UserPresenceService>();
builder.Services.AddScoped<IGameServerRepository, GameServerRepository>();
builder.Services.AddScoped<IPollRepository, PollRepository>();
builder.Services.AddScoped<IContainerManagementService, ContainerManagementService>();
builder.Services.AddScoped<IVolumeManagementService, VolumeManagementService>();
builder.Services.AddScoped<IImageManagementService, ImageManagementService>();
builder.Services.AddScoped<IGameServerQueryService, GameServerQueryService>();
builder.Services.AddHostedService<GameServerStatusMonitorService>();

// Register strategies
builder.Services.AddScoped<A2SGoldSourceStrategy>();
builder.Services.AddScoped<NoDetailsStrategy>();

// Register strategy factory
builder.Services.AddScoped<IGameServerInfoStrategyFactory, GameServerInfoStrategyFactory>();

// Register main query service (which uses the factory)
builder.Services.AddScoped<IGameServerQueryService, GameServerQueryService>();

builder.Services.AddHostedService<GameServerStatusMonitorService>();


builder.Services.AddControllers();
builder.Services.AddSignalR(); // Configure SignalR
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
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
            new string[] { }
        }
    });
});

var app = builder.Build();

// Seed database
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
        logger.LogError(ex, "Error while seeding the database.");
    }
}

async Task SeedRolesAndAdminAsync(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager,
    ILogger<Program> logger, IServiceProvider services)
{
    string[] roleNames =
    {
        RespawnApi.Domain.Enums.UserRoles.Administrator, RespawnApi.Domain.Enums.UserRoles.Spravce,
        RespawnApi.Domain.Enums.UserRoles.Uzivatel
    };
    foreach (var roleName in roleNames)
    {
        var roleExist = await roleManager.RoleExistsAsync(roleName);
        if (!roleExist)
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
            logger.LogInformation("Role '{RoleName}' was created.", roleName);
        }
    }

    var adminUser = await userManager.FindByNameAsync("patricek");
    if (adminUser == null)
    {
        var newAdmin = new IdentityUser
            { UserName = "patricek", Email = "patrik.mrnka12@gmail.com", EmailConfirmed = true };
        var createAdminResult = await userManager.CreateAsync(newAdmin, "a1234");
        if (createAdminResult.Succeeded)
        {
            await userManager.AddToRoleAsync(newAdmin, RespawnApi.Domain.Enums.UserRoles.Administrator);
            logger.LogInformation("User 'patricek' was created and assigned to the Administrator role.");

            // Create UserProfile for the admin
            var userProfileRepository =
                services.GetRequiredService<RespawnApi.DataAccess.Interfaces.IUserProfileRepository>();
            var adminProfile = new RespawnApi.Domain.Entities.UserProfile
                { UserId = newAdmin.Id, Nickname = newAdmin.UserName!, AvatarUrl = null };
            await userProfileRepository.AddAsync(adminProfile);
            logger.LogInformation("UserProfile for 'patricek' was created.");
        }
        else
        {
            foreach (var error in createAdminResult.Errors)
            {
                logger.LogError("Error while creating user 'patricek': {ErrorDescription}", error.Description);
            }
        }
    }
    else
    {
        logger.LogInformation("User 'patricek' already exists.");
        if (!await userManager.IsInRoleAsync(adminUser, RespawnApi.Domain.Enums.UserRoles.Administrator))
        {
            await userManager.AddToRoleAsync(adminUser, RespawnApi.Domain.Enums.UserRoles.Administrator);
            logger.LogInformation("User 'patricek' was assigned to the Administrator role.");
        }
    }
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowRespawnApp");

app.UseAuthentication(); // Must be before UseAuthorization
app.UseAuthorization();

app.MapControllers();
app.MapHub<PollHub>("/pollHub");
app.MapHub<PresenceHub>("/presenceHub");
app.MapHub<GameServerHub>("/gameServerHub");
app.MapHub<ServerLogHub>("/serverLogHub");

app.Run();