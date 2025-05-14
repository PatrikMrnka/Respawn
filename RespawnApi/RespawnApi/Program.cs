using Microsoft.EntityFrameworkCore;
using RespawnApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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

builder.Services.AddControllers();



var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
