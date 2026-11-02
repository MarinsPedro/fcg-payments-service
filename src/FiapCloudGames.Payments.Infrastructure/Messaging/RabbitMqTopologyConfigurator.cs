using FiapCloudGames.Payments.Contracts.Events.V1;
using MassTransit;

namespace FiapCloudGames.Payments.Infrastructure.Messaging;

public static class RabbitMqTopologyConfigurator
{
    public static void Configure(
        IRabbitMqBusFactoryConfigurator rabbitMq,
        RabbitMqExchangeOptions exchanges)
    {
        rabbitMq.Message<PaymentRequestedEventV1>(configuration =>
            configuration.SetEntityName(exchanges.PaymentRequested));
        rabbitMq.Message<PaymentApprovedEventV1>(configuration =>
            configuration.SetEntityName(exchanges.PaymentApproved));
        rabbitMq.Message<PaymentRejectedEventV1>(configuration =>
            configuration.SetEntityName(exchanges.PaymentRejected));
    }
}
