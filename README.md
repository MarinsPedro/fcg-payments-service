# Fiap Cloud Games - Payments Service

Microserviço responsável por registrar e processar pagamentos da plataforma Fiap Cloud Games.

## Fluxo atual

O fluxo possui duas etapas distintas:

1. **Orders publica `PaymentRequestedEvent`** no RabbitMQ.
2. **Payments Worker registra a intenção de pagamento** com status `Pending`.
3. O pagamento é processado posteriormente pela API HTTP (`POST /internal/v1/payments/{id}/confirm`), simulando a confirmação de PIX/QR Code/cartão.
4. O resultado é persistido com Outbox e publicado como `PaymentPaidEvent` ou `PaymentFailedEvent`.
5. RabbitMQ entrega uma cópia do resultado para as filas de Orders e Notifications.

```text
Orders
  |
  | PaymentRequestedEvent
  v
fcg.events (payments.requested)
  |
  v
Payments Worker
  |
  | registra Payment = Pending
  v
PostgreSQL

[confirmação HTTP posterior]
  |
  v
FakePaymentProvider (90% Paid / 10% Failed)
  |
  v
Outbox -> fcg.events
          | payments.paid / payments.failed
          +--> fcg.orders.payment-status
          +--> fcg.notifications.payment-status
```

O Payments **não publica diretamente para Library/Entitlements**. O Orders recebe o resultado do pagamento e publica o evento de negócio adequado para Library/Entitlements.

## Projetos

```text
src/
├── FiapCloudGames.Payments.Api
├── FiapCloudGames.Payments.Worker
├── FiapCloudGames.Payments.Application
├── FiapCloudGames.Payments.Domain
├── FiapCloudGames.Payments.Infrastructure
├── FiapCloudGames.Payments.Contracts
├── FiapCloudGames.Payments.Migrations
└── FiapCloudGames.Payments.Presentation
```

## Contrato de entrada

Exchange:

```text
fcg.events
```

Routing key:

```text
payments.requested
```

Fila consumida:

```text
fcg.payments.payment-requested
```

Payload:

```json
{
  "orderId": "55555555-1111-1111-1111-111111111111",
  "userId": "22222222-2222-2222-2222-222222222222",
  "amount": 99.90,
  "idempotencyKey": "ORDER-55555555-PAYMENT",
  "occurredAtUtc": "2026-10-18T18:00:00Z"
}
```

`MessageId` e `CorrelationId` são enviados nas propriedades da mensagem RabbitMQ. O `CorrelationId` recebido é persistido no pagamento e reutilizado nos eventos de saída, mesmo quando o processamento ocorre posteriormente por HTTP.

## Eventos de saída

### PaymentPaidEvent

Routing key:

```text
payments.paid
```

### PaymentFailedEvent

Routing key:

```text
payments.failed
```

Ambos são entregues para:

```text
fcg.orders.payment-status
fcg.notifications.payment-status
```

## Idempotência

A chave `IdempotencyKey` impede que uma redelivery de `PaymentRequestedEvent` crie outro pagamento para a mesma operação.

Padrão recomendado:

```text
ORDER-{OrderId}-PAYMENT
```

## Processamento HTTP

O Worker apenas registra a intenção `Pending`.

Para processar o pagamento:

```http
POST /internal/v1/payments/{paymentId}/confirm
```

Para localizar o pagamento pelo pedido:

```http
GET /internal/v1/payments/by-order/{orderId}
```

O `FakePaymentProvider` usa uma taxa configurável:

```json
{
  "FakePaymentProvider": {
    "ApprovalRate": 90
  }
}
```

- `100`: sempre aprova;
- `0`: sempre recusa;
- `90`: 90% Paid e 10% Failed.

## Banco

Database:

```text
fcg_payments
```

Schema:

```text
payments
```

Tabelas principais:

```text
payments.payments
payments.outbox_messages
```

Não há foreign keys para outros microsserviços.

## Outbox

O resultado do pagamento e a mensagem de saída são salvos no mesmo banco. O Worker publica mensagens pendentes no RabbitMQ e só preenche `processed_at_utc` após confirmação do broker.

## DLQ

Mensagens de entrada inválidas são encaminhadas para:

```text
fcg.payments.payment-requested.dlq
```

## Autenticação da API

Audience:

```text
payments-api
```

Scopes:

```text
payments.create
payments.read
payments.confirm
payments.cancel
```

## Desenvolvimento

```powershell
dotnet restore .\FiapCloudGames.Payments.slnx --configfile .\NuGet.Config
dotnet build .\FiapCloudGames.Payments.slnx
dotnet test .\FiapCloudGames.Payments.slnx
```

## Execução integrada

Docker Compose e Kubernetes são mantidos no repositório `fcg-orchestration`.
