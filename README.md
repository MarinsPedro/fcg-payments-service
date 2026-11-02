# Fiap Cloud Games - Payments Service

Microserviço responsável por registrar e processar pagamentos da plataforma Fiap Cloud Games.

## Fluxo atual

1. Orders publica `PaymentRequestedEventV1` em `payments.payment-requested.v1` pelo MassTransit.
2. O Payments Worker consome a fila `fcg-payments-payment-requested` e registra o pagamento como `Pending`.
3. `POST /internal/v1/payments/{id}/confirm` processa a confirmação simulada do pagamento.
4. O resultado é publicado como `PaymentApprovedEventV1` ou `PaymentRejectedEventV1`, usando o Outbox do MassTransit.
5. A saga de Orders e o Notifications Worker consomem os eventos pelas respectivas filas vinculadas automaticamente.

```text
Orders (MassTransit publish)
  -> payments.payment-requested.v1
     -> fcg-payments-payment-requested (Payments Worker)
        -> pagamento Pending (PostgreSQL)
           -> confirmação HTTP posterior (Payments API)
              -> payments.payment-approved.v1 OU payments.payment-rejected.v1
                 -> Orders Saga
                 -> Notifications Worker
```

O Payments **não publica diretamente para Library**: essa etapa é coordenada pela saga de Orders.

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

- Evento: `PaymentRequestedEventV1`
- Exchange MassTransit: `payments.payment-requested.v1` (fanout)
- Fila: `fcg-payments-payment-requested`
- URN: `urn:message:PaymentRequestedEventV1`

Payload simplificado:

```json
{
  "eventId": "bbbbbbbb-1111-1111-1111-111111111111",
  "orderId": "55555555-1111-1111-1111-111111111111",
  "userId": "22222222-2222-2222-2222-222222222222",
  "amount": 99.90,
  "idempotencyKey": "ORDER-55555555-PAYMENT",
  "occurredAtUtc": "2026-10-18T18:00:00Z"
}
```

O MassTransit usa `MessageId` e `CorrelationId` nos metadados da mensagem. O `CorrelationId` recebido é persistido no pagamento e reutilizado nas publicações subsequentes.

## Eventos de saída

| Evento | Exchange MassTransit |
|---|---|
| `PaymentApprovedEventV1` | `payments.payment-approved.v1` |
| `PaymentRejectedEventV1` | `payments.payment-rejected.v1` |

Os consumidores de Orders e Notifications criam as próprias filas e bindings ao iniciar. Não é necessário importar `definitions.json`, declarar `topic` ou configurar routing keys manualmente.

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

O resultado do pagamento e a mensagem de saída são persistidos de forma transacional. O Bus Outbox do MassTransit entrega os eventos ao RabbitMQ depois da confirmação do banco.

## DLQ

Erros persistentes de consumo são gerenciados pelo MassTransit, incluindo a fila de erro do endpoint (`fcg-payments-payment-requested_error`, conforme a configuração padrão). Não existe uma DLQ manual criada pelo orchestration.

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
