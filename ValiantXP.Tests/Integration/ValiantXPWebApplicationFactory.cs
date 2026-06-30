using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ValiantXP.Infrastructure.Data;

namespace ValiantXP.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that replaces SQL Server with an in-memory database.
/// Allows real HTTP pipeline tests without requiring a database connection.
/// </summary>
public class ValiantXPWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real SQL Server DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Also remove any registered SqlServer-specific options
            var sqlDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions));
            if (sqlDescriptor != null)
                services.Remove(sqlDescriptor);

            // Add in-memory database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase($"ValiantXP_Test_{Guid.NewGuid()}");
            });
        });

        builder.UseEnvironment("Testing");
    }
}
