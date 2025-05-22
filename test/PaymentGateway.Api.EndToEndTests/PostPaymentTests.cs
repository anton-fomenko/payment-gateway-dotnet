using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Configuration;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Domain;

using System.Net;
using System.Net.Http.Json;

namespace PaymentGateway.Api.EndToEndTests
{
    [Trait("Category", "EndToEnd")]
    public class PostPaymentTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public PostPaymentTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.Configure<BankOptions>(options =>
                    {
                        options.BaseUri = "https://bank-simulator-585c5811fce8.herokuapp.com";
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task PostPaymentAsync_AuthorizedPayment_ReturnsCreated()
        {
            // Arrange
            var request = new PostPaymentRequest
            {
                CardNumber = "2222405343248877", // Authorized test card
                ExpiryMonth = 4,
                ExpiryYear = 2025,
                Currency = "GBP",
                Amount = 100,
                Cvv = "123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var responseBody = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
            responseBody.Should().NotBeNull();
            responseBody!.Status.Should().Be(PaymentStatus.Authorized.ToString());
        }

        [Fact]
        public async Task PostPaymentAsync_DeclinedPayment_ReturnsCreated()
        {
            // Arrange
            var request = new PostPaymentRequest
            {
                CardNumber = "2222405343248112", // Declined test card
                ExpiryMonth = 1,
                ExpiryYear = 2026,
                Currency = "USD",
                Amount = 60000,
                Cvv = "456"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var responseBody = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
            responseBody.Should().NotBeNull();
            responseBody!.Status.Should().Be(PaymentStatus.Declined.ToString());
        }

        [Fact]
        public async Task PostPaymentAsync_BankUnavailable_ReturnsBankErrorStatus()
        {
            // Arrange
            var mockBankUrl = "http://localhost:5001/unavailable-bank-url";

            var factoryWithMockBank = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    var inMemorySettings = new Dictionary<string, string?>
                    {
                            { "BankOptions:Url", mockBankUrl }
                    };
                    config.AddInMemoryCollection(inMemorySettings);
                });
            });

            var clientWithMockBank = factoryWithMockBank.CreateClient();

            var request = new PostPaymentRequest
            {
                CardNumber = "0000000000000000",
                ExpiryMonth = 1,
                ExpiryYear = 2026,
                Currency = "EUR",
                Amount = 100,
                Cvv = "789"
            };

            // Act
            var response = await clientWithMockBank.PostAsJsonAsync("/api/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var responseBody = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
            responseBody.Should().NotBeNull();
            responseBody!.Status.Should().Be("BankError");
            responseBody.CardNumberLastFour.Should().Be("0000");
        }
    }
}
