using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VisitorManagement.Application.Interfaces;

namespace VisitorManagement.Infrastructure.Messaging;

public static class QueueServiceRegistration
{
    public static IServiceCollection AddVisitorQueueMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration[
            "AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException(
                "Azure Storage connection string is missing.");

        services.AddSingleton(_ =>
            new QueueServiceClient(
                connectionString,
                new QueueClientOptions
                {
                    MessageEncoding = QueueMessageEncoding.Base64
                }));

        services.AddScoped<
            IVisitorEventPublisher,
            AzureQueueVisitorEventPublisher>();

        return services;
    }
}