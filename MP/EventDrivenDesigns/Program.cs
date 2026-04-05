using EventDrivenDesigns;
using EventDrivenDesigns.Bus;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("[START] Event-Driven Architecture - RabbitMQ\n");

        using (var eventBus = new RabbitMQEventBus(
            hostName: "localhost",
            port: 5672,
            userName: "admin",
            password: "admin"))
        {
            // Descomenta para limpiar todas las colas
            //eventBus.DeleteAllQueues();
            // return;

            var example = new SportsbookEventDrivenExample(eventBus);
            await example.RunExampleAsync();
        }

        Console.WriteLine("[OK] Presiona cualquier tecla para salir...");
        Console.ReadKey();
    }
}


