using ECommerce.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using ECommerce.Infrastructure.Data;
using DotNetEnv;

// Ortam deðiþkenlerini yükle
DotNetEnv.Env.Load();

// Serilog yapýlandýrmasý
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/worker-log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Worker...");

    Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((hostContext, services) =>
        {
            // PostgreSQL baðlantý bilgilerini .env'den al
            var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
            var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "ECommerceDb";
            var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
            var dbPass = Environment.GetEnvironmentVariable("DB_PASS") ?? "postgres";

            var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass}";

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddHostedService<Worker>();
        })
        .Build()
        .Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
