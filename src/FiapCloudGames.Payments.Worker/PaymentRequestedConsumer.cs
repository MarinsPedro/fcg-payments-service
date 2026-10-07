using System.Text.Json;
using FiapCloudGames.Payments.Application.Features.Payments.Confirm;
using FiapCloudGames.Payments.Application.Features.Payments.Create;
using FiapCloudGames.Payments.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FiapCloudGames.Payments.Worker;

public sealed class PaymentRequestedConsumer(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<PaymentRequestedConsumer> logger)
    : BackgroundService
{
    private const string QueueName =
        "fcg.payments.payment-requested";

    private const string RoutingKey =
        "payments.requested";

    private const string DeadLetterExchange =
        "fcg.dead-letter";

    private const string DeadLetterQueue =
        "fcg.payments.payment-requested.dlq";

    private const string DeadLetterRoutingKey =
        "payments.requested.dead";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private IConnection? _connection;
    private IChannel? _channel;

    private readonly string _rabbitHost =
        configuration["RabbitMq:Host"] ?? "localhost";

    private readonly int _rabbitPort =
        int.TryParse(configuration["RabbitMq:Port"], out var port)
            ? port
            : 5672;

    private readonly string _rabbitUsername =
        configuration["RabbitMq:Username"] ?? "guest";

    private readonly string _rabbitPassword =
        configuration["RabbitMq:Password"] ?? "guest";

    private readonly string _exchange =
        configuration["RabbitMq:Exchange"] ?? "fcg.events";

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await ConnectAsync(stoppingToken);

        if (_channel is null)
            throw new InvalidOperationException(
                "Canal RabbitMQ não foi inicializado.");

        var consumer =
            new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            await HandleMessageAsync(
                eventArgs,
                stoppingToken);
        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation(
            "PaymentRequestedConsumer aguardando mensagens em {Queue}.",
            QueueName);

        await Task.Delay(
            Timeout.Infinite,
            stoppingToken);
    }

    private async Task HandleMessageAsync(
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        if (_channel is null)
            return;

        try
        {
            var paymentRequested =
                JsonSerializer.Deserialize<PaymentRequestedEvent>(
                    eventArgs.Body.Span,
                    JsonOptions)
                ?? throw new JsonException(
                    "Payload de PaymentRequestedEvent inválido.");

            using var scope =
                scopeFactory.CreateScope();

            var correlationContext =
                scope.ServiceProvider
                    .GetRequiredService<MessageCorrelationContext>();

            correlationContext.Set(
                eventArgs.BasicProperties.CorrelationId);

            var createService =
                scope.ServiceProvider
                    .GetRequiredService<CreatePaymentService>();

            var confirmService =
                scope.ServiceProvider
                    .GetRequiredService<ConfirmPaymentService>();

            logger.LogInformation(
                "Processando PaymentRequestedEvent. " +
                "OrderId={OrderId}, MessageId={MessageId}, CorrelationId={CorrelationId}.",
                paymentRequested.OrderId,
                eventArgs.BasicProperties.MessageId,
                correlationContext.CorrelationId);

            var createdPayment =
                await createService.ExecuteAsync(
                    new CreatePaymentInput(
                        paymentRequested.OrderId,
                        paymentRequested.UserId,
                        paymentRequested.Amount,
                        paymentRequested.IdempotencyKey),
                    cancellationToken);

            var processedPayment =
                await confirmService.ExecuteAsync(
                    createdPayment.Id,
                    cancellationToken);

            await _channel.BasicAckAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "PaymentRequestedEvent processado. " +
                "PaymentId={PaymentId}, Status={Status}.",
                processedPayment.Id,
                processedPayment.Status);
        }
        catch (JsonException ex)
        {
            logger.LogError(
                ex,
                "Mensagem PaymentRequestedEvent inválida. " +
                "MessageId={MessageId}. Enviando para DLQ.",
                eventArgs.BasicProperties.MessageId);

            await _channel.BasicNackAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false,
                requeue: false,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Falha ao processar PaymentRequestedEvent. " +
                "MessageId={MessageId}. Mensagem será reenfileirada.",
                eventArgs.BasicProperties.MessageId);

            await _channel.BasicNackAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false,
                requeue: true,
                cancellationToken: cancellationToken);
        }
    }

    private async Task ConnectAsync(
        CancellationToken cancellationToken)
    {
        var factory =
            new ConnectionFactory
            {
                HostName = _rabbitHost,
                Port = _rabbitPort,
                UserName = _rabbitUsername,
                Password = _rabbitPassword,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                ClientProvidedName =
                    "fcg-payments-payment-requested-consumer"
            };

        _connection =
            await factory.CreateConnectionAsync(
                cancellationToken);

        _channel =
            await _connection.CreateChannelAsync(
                cancellationToken: cancellationToken);

        await ConfigureTopologyAsync(
            cancellationToken);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "PaymentRequestedConsumer conectado ao RabbitMQ.");
    }

    private async Task ConfigureTopologyAsync(
        CancellationToken cancellationToken)
    {
        if (_channel is null)
            throw new InvalidOperationException(
                "Canal RabbitMQ não foi inicializado.");

        await _channel.ExchangeDeclareAsync(
            exchange: _exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: DeadLetterExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: DeadLetterQueue,
            exchange: DeadLetterExchange,
            routingKey: DeadLetterRoutingKey,
            cancellationToken: cancellationToken);

        var queueArguments =
            new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] =
                    DeadLetterExchange,

                ["x-dead-letter-routing-key"] =
                    DeadLetterRoutingKey
            };

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: QueueName,
            exchange: _exchange,
            routingKey: RoutingKey,
            cancellationToken: cancellationToken);
    }

    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();

        await base.StopAsync(
            cancellationToken);
    }
}