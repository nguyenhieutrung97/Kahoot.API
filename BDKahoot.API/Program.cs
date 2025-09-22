using BDKahoot.API.Extensions;
using BDKahoot.API.Hubs;
using BDKahoot.API.Middlewares;
using BDKahoot.API.Services.ActiveGameService;
using BDKahoot.Application.Extensions;
using BDKahoot.Application.Services.BlobStorageServices;
using BDKahoot.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Setup Proxy while using BOSCH internet (for local env).
//builder.LocalUseProxy();

// Load options
//builder.Services.Configure<BlobStorageOptions>(builder.Configuration.GetSection("BlobStorage"));

// Add Cors.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .WithOrigins("https://bdkahoot.trungtero.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add JSON options
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// Add Authentication, Authorization with SignalR support.
//builder.Services.AddJwtAuthenticationExtensions(builder.Configuration);

// Add Swagger.
builder.Services.AddSwagger();

// Add SignalR.
builder.Services.AddSignalR(options => { options.EnableDetailedErrors = true; });

// Add custom services
builder.Services.AddSingleton<IActiveGameService, ActiveGameService>();

// Add Hub services
builder.Services.AddHubServices();

//// Add Azure Services.
//builder.AddAzureServices();

// Add services from Application Layer.
builder.Services.AddApplication();

// Add services from Infrastructure Layer.
builder.Services.AddInfrastructure(builder.Configuration);

// Add middlewares
builder.Services.AddScoped<ErrorHandlingMiddleware>();
builder.Services.AddScoped<IClaimsTransformation, PostLoginClaimsTransformation>();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Local")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

//app.UseHttpsRedirection();

//app.UseAuthentication();

//app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapHub<GameHub>("/gameHub");

app.Run();

// Add for Integration Tests configurations.
public partial class Program { }