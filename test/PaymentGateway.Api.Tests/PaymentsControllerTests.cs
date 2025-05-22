using AutoFixture;

using AutoMapper;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Moq;

using PaymentGateway.Api.Configuration;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Mappings;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Utilities;
using PaymentGateway.Domain;
using PaymentGateway.Services;

namespace PaymentGateway.Api.UnitTests
{
    public class PaymentsControllerTests
    {
        private readonly Mock<IPaymentsService> _paymentsServiceMock;
        private readonly IMemoryCache _memoryCache;
        private readonly PaymentsController _sut;

        public PaymentsControllerTests()
        {
            DateTimeProvider.SetUtcNow(new DateTime(2021, 1, 31, 15, 0, 0));
            _paymentsServiceMock = new Mock<IPaymentsService>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            var idempotencyOptions = Options.Create(new IdempotencyOptions() {  TimeoutHours = 24 });

            // Configure AutoMapper with the same MappingProfile used in the application
            var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile(new MappingProfile()));

            var mapper = mapperConfig.CreateMapper();

            _sut = new PaymentsController(_paymentsServiceMock.Object, _memoryCache, idempotencyOptions, mapper);
        }

        [Fact]
        public async Task PostPaymentAsync_ValidRequest_ReturnsAuthorizedStatusWithPaymentDetails()
        {
            // Arrange
            var fixture = new Fixture();
            var request = fixture.Build<PostPaymentRequest>()
                                 .With(x => x.CardNumber, "1234567812345678")
                                 .With(x => x.Cvv, "123")
                                 .With(x => x.ExpiryMonth, 12)
                                 .With(x => x.ExpiryYear, DateTimeProvider.UtcNow.Year + 1)
                                 .With(x => x.Currency, "USD")
                                 .With(x => x.Amount, 1000)
                                 .Create();

            var expectedPaymentResponse = new Payment
            {
                Id = Guid.NewGuid(),
                Status = PaymentStatus.Authorized,
                CardNumber = request.CardNumber!,
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Currency = request.Currency!,
                Amount = request.Amount,
                Cvv = request.Cvv!
            };

            _paymentsServiceMock.Setup(service => service.ProcessPaymentAsync(It.IsAny<Payment>()))
                                .ReturnsAsync(expectedPaymentResponse);

            // Act
            var actionResult = await _sut.PostPaymentAsync(request);

            // Assert
            actionResult.Should().NotBeNull();
            actionResult.Result.Should().BeOfType<CreatedAtRouteResult>();

            var createdAtRouteResult = (CreatedAtRouteResult?)actionResult.Result;
            createdAtRouteResult!.RouteName.Should().Be("GetPaymentAsync");
            createdAtRouteResult.Value.Should().BeOfType<PostPaymentResponse>();

            var paymentResponse = (PostPaymentResponse?)createdAtRouteResult.Value;
            paymentResponse!.Should().NotBeNull();
            paymentResponse!.Id.Should().Be(expectedPaymentResponse.Id);
            paymentResponse.Status.Should().Be(PaymentStatus.Authorized.ToString());
            paymentResponse.CardNumberLastFour.Should().Be("5678");
            paymentResponse.ExpiryMonth.Should().Be(request.ExpiryMonth);
            paymentResponse.ExpiryYear.Should().Be(request.ExpiryYear);
            paymentResponse.Currency.Should().Be(request.Currency);
            paymentResponse.Amount.Should().Be(request.Amount);
        }

        [Fact]
        public async Task PostPaymentAsync_WithIdempotencyKey_ReturnsCachedResponse()
        {
            // Arrange
            var idempotencyKey = Guid.NewGuid().ToString();
            var request = new PostPaymentRequest
            {
                CardNumber = "2222405343248877",
                ExpiryMonth = 4,
                ExpiryYear = 2025,
                Currency = "GBP",
                Amount = 100,
                Cvv = "123"
            };

            var cachedPaymentResponse = new PostPaymentResponse
            {
                Id = Guid.NewGuid(),
                Status = PaymentStatus.Authorized.ToString(),
                CardNumberLastFour = "8877",
                ExpiryMonth = 4,
                ExpiryYear = 2025,
                Currency = "GBP",
                Amount = 100
            };

            _memoryCache.Set(idempotencyKey, cachedPaymentResponse, TimeSpan.FromMinutes(10));

            // Act
            var actionResult = await _sut.PostPaymentAsync(request, idempotencyKey);

            // Assert
            actionResult.Should().NotBeNull();
            actionResult.Result.Should().BeOfType<CreatedAtRouteResult>();

            var okResult = (CreatedAtRouteResult?)actionResult.Result;
            okResult!.Value.Should().BeOfType<PostPaymentResponse>();

            var paymentResponse = (PostPaymentResponse?)okResult.Value;
            paymentResponse.Should().BeEquivalentTo(cachedPaymentResponse);

            // Verify that ProcessPaymentAsync was never called since the response was cached
            _paymentsServiceMock.Verify(
                service => service.ProcessPaymentAsync(It.IsAny<Payment>()),
                Times.Never);
        }

        [Fact]
        public async Task PostPaymentAsync_WithIdempotencyKey_NotCached_ProcessesAndCachesPayment()
        {
            // Arrange
            var idempotencyKey = Guid.NewGuid().ToString();
            var request = new PostPaymentRequest
            {
                CardNumber = "2222405343248877",
                ExpiryMonth = 4,
                ExpiryYear = 2025,
                Currency = "GBP",
                Amount = 100,
                Cvv = "123"
            };

            var processedPayment = new Payment
            {
                Id = Guid.NewGuid(),
                Status = PaymentStatus.Authorized,
                CardNumber = request.CardNumber,
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Currency = request.Currency,
                Amount = request.Amount,
                Cvv = request.Cvv
            };

            var expectedPostResponse = new PostPaymentResponse
            {
                Id = processedPayment.Id,
                Status = PaymentStatus.Authorized.ToString(),
                CardNumberLastFour = "8877",
                ExpiryMonth = processedPayment.ExpiryMonth,
                ExpiryYear = processedPayment.ExpiryYear,
                Currency = processedPayment.Currency,
                Amount = processedPayment.Amount
            };

            // Setup the PaymentsService to return the processed payment
            _paymentsServiceMock.Setup(service => service.ProcessPaymentAsync(It.IsAny<Payment>()))
                                .ReturnsAsync(processedPayment);

            // Act
            var actionResult = await _sut.PostPaymentAsync(request, idempotencyKey);

            // Assert
            actionResult.Should().NotBeNull();
            actionResult.Result.Should().BeOfType<CreatedAtRouteResult>();

            var createdAtRouteResult = (CreatedAtRouteResult?)actionResult.Result;
            createdAtRouteResult!.RouteName.Should().Be("GetPaymentAsync");
            createdAtRouteResult.Value.Should().BeOfType<PostPaymentResponse>();

            var paymentResponse = (PostPaymentResponse?)createdAtRouteResult.Value;
            paymentResponse.Should().BeEquivalentTo(expectedPostResponse);

            // Verify that ProcessPaymentAsync was called once since the response was not cached
            _paymentsServiceMock.Verify(
                service => service.ProcessPaymentAsync(It.IsAny<Payment>()),
                Times.Once);

            // Verify that the response was cached
            _memoryCache.TryGetValue(idempotencyKey, out PostPaymentResponse? cachedResponse).Should().BeTrue();
            cachedResponse.Should().BeEquivalentTo(expectedPostResponse);
        }
    }
}
