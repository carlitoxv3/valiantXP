using Azure.Identity;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using ValiantXP.API.Extensions;
using ValiantXP.Application;
using ValiantXP.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var appConfigConnectionString = builder.Configuration.GetConnectionString("AppConfig");
if (!string.IsNullOrEmpty(appConfigConnectionString))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(appConfigConnectionString)
               .ConfigureKeyVault(kv => kv.SetCredential(new DefaultAzureCredential()));
    });
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    options.AddPolicy("OtpPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
            
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Custom services registration
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Auth & Swagger Configuration
builder.Services.AddCustomAuthentication(builder.Configuration);
builder.Services.AddSwaggerGenWithAuth();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ValiantXP API V1");
    });
}

app.UseHttpsRedirection();

app.UseRateLimiter();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
