using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc;

using Moq;

using PaymentGateway.Api.IntegrationTests.Setup;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Utilities;
using PaymentGateway.Domain;
using PaymentGateway.Services;

namespace PaymentGateway.Api.IntegrationTests
{
    public class ValidationTests : IClassFixture<TestWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly TestWebApplicationFactory<Program> _factory;
        private readonly Mock<IPaymentsService> _paymentsServiceMock;

        public ValidationTests(TestWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _paymentsServiceMock = _factory.PaymentsServiceMock;
            _client = _factory.CreateClient();
            DateTimeProvider.SetUtcNow(new DateTime(2021, 1, 31, 15, 0, 0));
        }

        [Fact]
        public async Task PostPaymentAsync_ValidRequest_ReturnsAuthorizedPayment()
        {
            // Arrange
            var request = new PostPaymentRequest
            {
                CardNumber = "1234567890123456",
                ExpiryMonth = 12,
                ExpiryYear = 2050,
                Currency = "USD",
                Amount = 1000,
                Cvv = "123"
            };

            var expectedPayment = new Payment
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

            _paymentsServiceMock.Setup(service => service.ProcessPaymentAsync(It.IsAny<Payment>()))
                .ReturnsAsync(expectedPayment);

            // Act
            var response = await _client.PostAsJsonAsync("/api/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var responseBody = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

            responseBody.Should().NotBeNull();
            responseBody!.Id.Should().Be(expectedPayment.Id);
            responseBody.Status.Should().Be(PaymentStatus.Authorized.ToString());
            responseBody.CardNumberLastFour.Should().Be("3456");
            responseBody.ExpiryMonth.Should().Be(expectedPayment.ExpiryMonth);
            responseBody.ExpiryYear.Should().Be(expectedPayment.ExpiryYear);
            responseBody.Currency.Should().Be(expectedPayment.Currency);
            responseBody.Amount.Should().Be(expectedPayment.Amount);
        }

        [Theory]
        [InlineData("1234567890123")] // 13 digits
        [InlineData("12345678901234567890")] // 20 digits
        [InlineData("1234abcd5678efgh")] // 20 digits
        public async Task PostPaymentAsync_InvalidCardNumber_ReturnsBadRequest(string cardNumber)
        {
            // Arrange
            var request = new PostPaymentRequest
            {
                CardNumber = cardNumber,
                ExpiryMonth = 12,
                ExpiryYear = 2030,
                Currency = "USD",
                Amount = 1000,
                Cvv = "123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Payment Rejected");
            problemDetails.Errors.Should().ContainKey("CardNumber");
            problemDetails.Errors["CardNumber"].Should().Contain("Card number must be between 14 and 19 digits.");
        }

        [Theory]
        [InlineData(0)]  // Less than 1
        [InlineData(13)] // Greater than 12
        public async Task PostPaymentAsync_InvalidExpiryMonth_ReturnsBadRequest(int expiryMonth)
        {
            // Arrange
            var request = new PostPaymentRequest
            {
                CardNumber = "1234567890123456",
                ExpiryMonth = expiryMonth,
                ExpiryYear = 2030,
                Currency = "USD",
                Amount = 1000,
                Cvv = "123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Payment Rejected");
            problemDetails.Errors.Should().ContainKey("ExpiryMonth");
            problemDetails.Errors["ExpiryMonth"].Should().Contain("Expiry month must be between 1 and 12.");
        }

        [Theory]
        [InlineData(1, 2021)]
        [InlineData(12, 2022)]
        public async Task PostPaymentAsync_ExpiryDateInFuture_ReturnsAuthorizedPayment(int expiryMonth, int expiryYear)
        {
            // Arrange
            DateTimeProvider.SetUtcNow(new DateTime(2021, 1, 31, 15, 0, 0));

            var request = new PostPaymentRequest
            {
                CardNumber = "1234567890123456",
                ExpiryMonth = expiryMonth,
                ExpiryYear = expiryYear,
                Currency = "USD",
                Amount = 1000,
                Cvv = "123"
            };

            var expectedResponse = new Payment
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

            _paymentsServiceMock.Setup(service => service.ProcessPaymentAsync(It.IsAny<Payment>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _client.PostAsJsonAsync("/api/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var responseBody = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

            responseBody.Should().NotBeNull();
            responseBody!.Id.Should().Be(expectedResponse.Id);
            responseBody.Status.Should().Be(PaymentStatus.Authorized.ToString());
            responseBody.CardNumberLastFour.Should().Be("3456");
            responseBody.ExpiryMonth.Should().Be(expectedResponse.ExpiryMonth);
            responseBody.ExpiryYear.Should().Be(expectedResponse.ExpiryYear);
            responseBody.Currency.Should().Be(expectedResponse.Currency);
            responseBody.Amount.Should().Be(expectedResponse.Amount);
        }

        [Theory]
        [InlineData(1, 2019)]  
        [InlineData(12, 2020)]
        public async Task PostPaymentAsync_ExpiryDateInPast_ReturnsBadRequest(int expiryMonth, int expiryYear)
        {
            // Arrange
            DateTimeProvider.SetUtcNow(new DateTime(2021, 1, 1));

            var request = new PostPaymentRequest
            {
                CardNumber = "1234567890123456",
                ExpiryMonth = expiryMonth,
                ExpiryYear = expiryYear,
                Currency = "USD",
                Amount = 1000,
                Cvv = "123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Payment Rejected");
            problemDetails.Errors.Should().ContainKey("ExpiryMonth");
            problemDetails.Errors.Should().ContainKey("ExpiryYear");
            problemDetails.Errors["ExpiryMonth"].Should().Contain("The card expiry date must be in the future.");
            problemDetails.Errors["ExpiryYear"].Should().Contain("The card expiry date must be in the future.");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(9999)]
        public async Task PostPaymentAsync_InvalidExpiryYear_ReturnsBadRequest(int expiryYear)
        {
            // Arrange
            DateTimeProvider.SetUtcNow(new DateTime(2021, 1, 1));

            var request = new PostPaymentRequest
            {
                CardNumber = "1234567890123456",
                ExpiryMonth = 12,
                ExpiryYear = expiryYear,
                Currency = "USD",
                Amount = 1000,
                Cvv = "123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Payment Rejected");
            problemDetails.Errors.Should().ContainKey("ExpiryYear");
            problemDetails.Errors["ExpiryYear"].Should().Contain("Expiry year must be a valid year in the future.");
        }

        [Theory]
        [InlineData("US")]    // Less than 3 characters
        [InlineData("USDT")]  // More than 3 characters
        [InlineData("XYZ")]   // Not in allowed currencies
        public async Task PostPaymentAsync_InvalidCurrency_ReturnsBadRequest(string currency)
        {
            // Arrange
            var request = new PostPaymentRequest
            {
                CardNumber = "1234567890123456",
                ExpiryMonth = 12,
                ExpiryYear = 2040,
                Currency = currency,
                Amount = 1000,
                Cvv = "123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Payment Rejected");
            problemDetails.Errors.Should().ContainKey("Currency");
            problemDetails.Errors["Currency"].Should().Contain($"Currency must be one of the following: USD, EUR, GBP.");
        }

        [Theory]
        [InlineData(0)]            
        [InlineData(-100)]     
        public async Task PostPaymentAsync_InvalidAmount_ReturnsBadRequest(int amount)
        {
            // Arrange
            var request = new PostPaymentRequest
            {
                CardNumber = "1234567890123456",
                ExpiryMonth = 12,
                ExpiryYear = 2030,
                Currency = "USD",
                Amount = amount,
                Cvv = "123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Payment Rejected");
            problemDetails.Errors.Should().ContainKey("Amount");
            problemDetails.Errors["Amount"].Should().Contain("Amount must be a positive integer.");
        }

        [Theory]
        [InlineData("12")]     // Less than 3 digits
        [InlineData("12345")]  // More than 4 digits
        [InlineData("12a4")]   // Non-numeric characters
        public async Task PostPaymentAsync_InvalidCvv_ReturnsBadRequest(string cvv)
        {
            // Arrange
            var request = new PostPaymentRequest
            {
                CardNumber = "1234567890123456",
                ExpiryMonth = 12,
                ExpiryYear = 2030,
                Currency = "USD",
                Amount = 1000,
                Cvv = cvv
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Payment Rejected");
            problemDetails.Errors.Should().ContainKey("Cvv");
            problemDetails.Errors["Cvv"].Should().Contain("CVV must be 3 or 4 digits.");
        }

        [Fact]
        public async Task PostPaymentAsync_MissingRequiredFields_ReturnsBadRequest()
        {
            // Arrange
            var request = new PostPaymentRequest
            {
                // All fields missing
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Payment Rejected");
            problemDetails.Errors.Should().ContainKey("CardNumber");
            problemDetails.Errors.Should().ContainKey("ExpiryMonth");
            problemDetails.Errors.Should().ContainKey("ExpiryYear");
            problemDetails.Errors.Should().ContainKey("Currency");
            problemDetails.Errors.Should().ContainKey("Amount");
            problemDetails.Errors.Should().ContainKey("Cvv");
        }

        [Fact]
        public async Task GetPaymentAsync_ValidId_ReturnsPaymentDetails()
        {
            // Arrange
            var paymentId = Guid.NewGuid();

            var payment = new Payment
            {
                Id = paymentId,
                Status = PaymentGateway.Domain.PaymentStatus.Authorized,
                CardNumber = "1234567890123456",
                ExpiryMonth = 12,
                ExpiryYear = 2030,
                Currency = "USD",
                Amount = 1000,
                Cvv = "123"
            };

            _paymentsServiceMock.Setup(service => service.GetPaymentAsync(paymentId))
                .ReturnsAsync(payment);

            // Act
            var response = await _client.GetAsync($"/api/payments/{paymentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseBody = await response.Content.ReadFromJsonAsync<GetPaymentResponse>();

            responseBody.Should().NotBeNull();
            responseBody!.Id.Should().Be(payment.Id);
            responseBody.Status.Should().Be(PaymentStatus.Authorized.ToString());
            responseBody.CardNumberLastFour.Should().Be("3456");
            responseBody.ExpiryMonth.Should().Be(payment.ExpiryMonth);
            responseBody.ExpiryYear.Should().Be(payment.ExpiryYear);
            responseBody.Currency.Should().Be(payment.Currency);
            responseBody.Amount.Should().Be(payment.Amount);
        }

        [Fact]
        public async Task GetPaymentAsync_InvalidId_ReturnsNotFound()
        {
            // Arrange
            var paymentId = Guid.NewGuid();

            _paymentsServiceMock.Setup(service => service.GetPaymentAsync(paymentId))
                .ReturnsAsync((Payment?)null);

            // Act
            var response = await _client.GetAsync($"/api/payments/{paymentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
