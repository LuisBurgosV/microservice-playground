using Producer.Services;
using RabbitMQ.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Producer;

public static class DependencyInjection
{
    public static IServiceCollection AddEventProducer(this IServiceCollection services)
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
            DispatchConsumersAsync = true
        };

        var connection = factory.CreateConnection();
        
        services.AddSingleton(connection);
        services.AddScoped<IEventPublisher, RabbitMQEventPublisher>();

        return services;
    }
}
