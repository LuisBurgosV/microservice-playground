using ServiceBusClient;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var serviceBusPublisher = new ServiceBusEventPublisher(
            connectionString: "Endpoint=sb://mp-servicebus-test-luis.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=9jH8pJ9ABi6R1ioCWiuCjVxA3dC3ZQoz5+ASbOHnwo4="
        );

        try
        {
            while (true)
            {
                Console.WriteLine("Send a message with ServiceBus");
                var message = Console.ReadLine();

                if (string.IsNullOrEmpty(message))
                    continue;

                var response = await serviceBusPublisher.PublishAsync<string>(
                    queueName: "servicebus-ollama-queue",
                    message: message,
                    ct: CancellationToken.None);

                Console.WriteLine("Response\n");
                Console.WriteLine(response);
            }
        }
        finally
        {
            await serviceBusPublisher.DisposeAsync();
        }
    }
}