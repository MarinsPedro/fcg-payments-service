using System.Text;
using FiapCloudGames.Payments.Application.Abstractions.Persistence;
using FiapCloudGames.Payments.Domain.Outbox;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace FiapCloudGames.Payments.Worker;

public sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<OutboxPublisherWorker> logger)
    : BackgroundService
{
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

    private readonly int _batchSize =
        int.TryParse(configuration["Outbox:BatchSize"], out var batch)
            ? batch
            : 20;

    private readonly TimeSpan _pollingInterval =
        TimeSpan.FromSeconds(
            int.TryParse(
                configuration["Outbox:PollingIntervalSeconds"],
                out var seconds)
                ? seconds
                : 2);

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Payments Outbox Publisher iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EnsureRabbitMqAsync(stoppingToken);

                await PublishPendingMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Erro no processamento da Outbox.");

                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    stoppingToken);

                continue;
            }

            await Task.Delay(
                _pollingInterval,
                stoppingToken);
        }
    }

    private async Task PublishPendingMessagesAsync(
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var outbox =
            scope.ServiceProvider
                .GetRequiredService<IOutboxRepository>();

        var unitOfWork =
            scope.ServiceProvider
                .GetRequiredService<IPaymentsUnitOfWork>();

        var messages = await outbox.GetPendingAsync(
            _batchSize,
            cancellationToken);

        if (messages.Count == 0)
            return;

        logger.LogInformation(
            "{Count} mensagem(ns) pendente(s) encontrada(s) na Outbox.",
            messages.Count);

        foreach (var message in messages)
        {
            try
            {
                await PublishAsync(
                    message,
                    cancellationToken);

                message.MarkProcessed(
                    DateTimeOffset.UtcNow);

                await unitOfWork.SaveChangesAsync(
                    cancellationToken);

                logger.LogInformation(
                    "Outbox {OutboxId} publicada. Evento {EventType}.",
                    message.Id,
                    message.EventType);
            }
            catch (Exception ex)
            {
                message.MarkFailed(ex.Message);

                await unitOfWork.SaveChangesAsync(
                    cancellationToken);

                logger.LogError(
                    ex,
                    "Falha ao publicar Outbox {OutboxId}.",
                    message.Id);
            }
        }
    }

    private async Task PublishAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        if (_channel is null || !_channel.IsOpen)
            throw new InvalidOperationException(
                "Canal RabbitMQ não está disponível.");

        var routingKey = message.EventType switch
        {
            "PaymentPaidEvent" =>
                "payments.paid",

            "PaymentFailedEvent" =>
                "payments.failed",

            _ => throw new InvalidOperationException(
                $"Evento não suportado: {message.EventType}")
        };

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            ContentEncoding = "utf-8",

            MessageId =
                message.Id.ToString(),

            CorrelationId =
                message.CorrelationId,

            Type =
                message.EventType,

            DeliveryMode =
                DeliveryModes.Persistent
        };

        var body =
            Encoding.UTF8.GetBytes(message.Payload);

        await _channel.BasicPublishAsync(
            exchange: _exchange,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    private async Task EnsureRabbitMqAsync(
        CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true } &&
            _channel is { IsOpen: true })
        {
            return;
        }

        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        var factory = new ConnectionFactory
        {
            HostName = _rabbitHost,
            Port = _rabbitPort,
            UserName = _rabbitUsername,
            Password = _rabbitPassword,

            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,

            ClientProvidedName =
                "fcg-payments-outbox-publisher"
        };

        _connection =
            await factory.CreateConnectionAsync(
                cancellationToken);

        var channelOptions =
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true);

        _channel =
            await _connection.CreateChannelAsync(
                channelOptions,
                cancellationToken);

        await ConfigureTopologyAsync(
            cancellationToken);

        logger.LogInformation(
            "Conectado ao RabbitMQ em {Host}:{Port}.",
            _rabbitHost,
            _rabbitPort);
    }

    private async Task ConfigureTopologyAsync(
        CancellationToken cancellationToken)
    {
        if (_channel is null)
            throw new InvalidOperationException(
                "Canal RabbitMQ não inicializado.");

        await _channel.ExchangeDeclareAsync(
            exchange: _exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        const string ordersQueue =
            "fcg.orders.payment-status";

        await _channel.QueueDeclareAsync(
            queue: ordersQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: ordersQueue,
            exchange: _exchange,
            routingKey: "payments.paid",
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: ordersQueue,
            exchange: _exchange,
            routingKey: "payments.failed",
            cancellationToken: cancellationToken);

        const string entitlementsQueue =
            "fcg.entitlements.payment-paid";

        await _channel.QueueDeclareAsync(
            queue: entitlementsQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: entitlementsQueue,
            exchange: _exchange,
            routingKey: "payments.paid",
            cancellationToken: cancellationToken);
    }

    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        if (_channel is not null)
            await _channel.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();

        await base.StopAsync(cancellationToken);
    }
}