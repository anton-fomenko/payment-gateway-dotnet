using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Validation;
using Microsoft.Extensions.Caching.Memory;
using PaymentGateway.Services;
using PaymentGateway.Domain;
using PaymentGateway.Api.Configuration;
using Microsoft.Extensions.Options;
using AutoMapper;


namespace PaymentGateway.Api.Controllers;

[Route("api/payments")]
[ApiController]
[ServiceFilter(typeof(ValidationFilter))]
public class PaymentsController(
    IPaymentsService paymentsService,
    IMemoryCache memoryCache,
    IOptions<IdempotencyOptions> idempotencyOptions,
    IMapper mapper) : ControllerBase
{
    private readonly IPaymentsService _paymentsService = paymentsService;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly IdempotencyOptions _idempotencyOptions = idempotencyOptions.Value;
    private readonly IMapper _mapper = mapper;

    [HttpGet("{id:guid}", Name = "GetPaymentAsync")]
    public async Task<ActionResult<GetPaymentResponse>> GetPaymentAsync(Guid id)
    {
        var payment = await _paymentsService.GetPaymentAsync(id);

        // Result pattern can be used here as well. For simplicity, I used null check here instead.
        if (payment == null)
        {
            return NotFound();
        }

        var response = _mapper.Map<GetPaymentResponse>(payment);

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> PostPaymentAsync(
        [FromBody] PostPaymentRequest request, 
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            if (_memoryCache.TryGetValue(idempotencyKey, out PostPaymentResponse? cachedResponse))
            {
                return CreatedAtRoute(
                    routeName: "GetPaymentAsync",
                    routeValues: new { id = cachedResponse!.Id },
                    value: cachedResponse
                );
            }
        }

        var payment = _mapper.Map<Payment>(request);
        var processedPayment = await _paymentsService.ProcessPaymentAsync(payment);
        var response = _mapper.Map<PostPaymentResponse>(processedPayment);

        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_idempotencyOptions.TimeoutHours)
            };

            _memoryCache.Set(idempotencyKey, response, cacheOptions);
        }

        return CreatedAtRoute(
            routeName: "GetPaymentAsync",
            routeValues: new { id = response.Id },
            value: response
        );
    }
}