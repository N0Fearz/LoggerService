using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;

namespace LoggerService.Services;

public class RabbitMqLogConsumer : BackgroundService
{

    private readonly string _queueName = "log-queue";
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    
    public RabbitMqLogConsumer(IConfiguration configuration)
    {
       var  _configuration = configuration;
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"],
            UserName = _configuration["RabbitMQ:UserName"],
            Password = _configuration["RabbitMQ:Password"]
        };
        
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        _channel.QueueBind(queue: _queueName, exchange: "amq.topic", routingKey: "logs.#");
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Start consumer
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            // Log ontvangen berichten
            Log.Information("Log ontvangen van RabbitMQ: {LogMessage}", message);
        };

        _channel.BasicConsume(queue: _queueName, autoAck: true, consumer: consumer);

        return Task.CompletedTask;
    }
    
    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}