using Fgc.Users.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<UsersDbContext>));
            services.RemoveAll(typeof(DbContextOptions));
            
            services.AddScoped<DbContextOptions<UsersDbContext>>(provider =>
            {
                return new DbContextOptionsBuilder<UsersDbContext>()
                    .UseInMemoryDatabase("InMemoryUsersTestDb")
                    .Options;
            });
        });
    }
}
