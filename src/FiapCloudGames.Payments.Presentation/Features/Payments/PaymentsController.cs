using FiapCloudGames.Payments.Application.Features.Payments.Confirm;
using FiapCloudGames.Payments.Application.Features.Payments.Create;
using FiapCloudGames.Payments.Application.Features.Payments.Get;
using FiapCloudGames.Payments.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Payments.Presentation.Features.Payments;

[ApiController]
[Route("internal/v1/payments")]
public sealed class PaymentsController : ControllerBase
{
    private const string GetByIdRoute = "Payments.GetById";

    [Authorize(Policy = PaymentsPolicies.Create)]
    [HttpPost]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PaymentResponse>> CreateAsync(
        [FromBody] CreatePaymentRequest request,
        [FromServices] CreatePaymentService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ExecuteAsync(
            request.ToInput(),
            cancellationToken);

        var response = PaymentResponse.From(result);

        return CreatedAtRoute(
            GetByIdRoute,
            new { id = response.Id },
            response);
    }

    [Authorize(Policy = PaymentsPolicies.Read)]
    [HttpGet("{id:guid}", Name = GetByIdRoute)]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentResponse>> GetAsync(Guid id, [FromServices] GetPaymentService service, CancellationToken cancellationToken) =>
        Ok(PaymentResponse.From(await service.GetByIdAsync(id, cancellationToken)));

    [Authorize(Policy = PaymentsPolicies.Read)]
    [HttpGet("by-order/{orderId:guid}")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentResponse>> GetByOrderAsync(Guid orderId, [FromServices] GetPaymentService service, CancellationToken cancellationToken) =>
        Ok(PaymentResponse.From(await service.GetByOrderIdAsync(orderId, cancellationToken)));

    [Authorize(Policy = PaymentsPolicies.Confirm)]
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentResponse>> ConfirmAsync(Guid id, [FromServices] ConfirmPaymentService service, CancellationToken cancellationToken) =>
        Ok(PaymentResponse.From(await service.ExecuteAsync(id, cancellationToken)));
}
