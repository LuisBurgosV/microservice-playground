using Listener;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient<IOllamaService, OllamaService>();

        services.AddSingleton<IRabbitMQListener>(sp =>
            new RabbitMQListener(
                host: "localhost",
                user: "admin",
                pass: "admin",
                ollamaService: sp.GetRequiredService<IOllamaService>()
            )
        );

        services.AddSingleton<IServiceBusListener>(sp =>
            new ServiceBusListener(
                connectionString: "Endpoint=sb://mp-servicebus-test-luis.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=XXXXXXXXXXXXXXX",
                ollamaService: sp.GetRequiredService<IOllamaService>()
            )
        );

        services.AddHostedService<ListenerService>();
    })
    .Build();

await host.RunAsync();

