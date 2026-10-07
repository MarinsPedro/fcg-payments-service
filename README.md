# Fiap Cloud Games - Payments Service

Microserviço responsável pelo processamento de pagamentos da plataforma Fiap Cloud Games.

O serviço utiliza arquitetura assíncrona orientada a eventos, PostgreSQL próprio, padrão Outbox e integração com RabbitMQ.

## Responsabilidades

O Payments Service é responsável por:

- receber solicitações de pagamento através do RabbitMQ;
- garantir idempotência no processamento;
- registrar pagamentos em banco próprio;
- processar pagamentos através de um provider;
- publicar eventos de pagamento aprovado ou recusado;
- disponibilizar consultas e operações administrativas através da API;
- manter rastreabilidade através de Correlation ID.

## Arquitetura

```text
Orders
  |
  | PaymentRequestedEvent
  v
RabbitMQ
  |
  v
Payments Worker
  |
  +--> PostgreSQL
  |
  +--> FakePaymentProvider
          |
          +--> 90% Paid
          |
          +--> 10% Failed
                  |
                  v
                Outbox
                  |
                  v
              RabbitMQ
             /        \
PaymentPaidEvent   PaymentFailedEvent