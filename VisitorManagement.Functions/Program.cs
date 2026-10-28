using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VisitorManagement.Infrastructure;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile(
            "local.settings.json",
            optional: true,
            reloadOnChange: false);

        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var connectionString =
            context.Configuration.GetConnectionString("DefaultConnection")
            ?? context.Configuration["Values:ConnectionStrings__DefaultConnection"]
            ?? context.Configuration["ConnectionStrings__DefaultConnection"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "SQL Server connection string is missing. " +
                "Check ConnectionStrings__DefaultConnection in local.settings.json.");
        }

        var infrastructureConfiguration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] =
                            connectionString
                    })
                .Build();

        services.AddInfrastructure(infrastructureConfiguration);
    })
    .Build();

host.Run();