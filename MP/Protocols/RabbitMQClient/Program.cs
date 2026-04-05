using RabbitMQClient;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var rabbitMQPublisher = new RabbitMQPublisher(
            host: "localhost",
            user: "admin",
            pass: "admin"
        );

        try
        {
            while (true)
            {
                Console.WriteLine("Send a message with RabbitMQ");
                var message = Console.ReadLine();

                if (string.IsNullOrEmpty(message))
                    continue;

                var response = await rabbitMQPublisher.PublishAsync<string>(
                    routingKey: "rabbitmq.ollama",
                    message: message,
                    ct: CancellationToken.None);

                Console.WriteLine("Response\n");
                Console.WriteLine(response);
            }
        }
        finally
        {
            await rabbitMQPublisher.DisposeAsync();
        }
    }
}