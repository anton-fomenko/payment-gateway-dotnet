using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Configuration;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Domain;

using System.Net;
using System.Net.Http.Json;

namespace PaymentGateway.Api.EndToEndTests
{
    public class GetPaymentTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public GetPaymentTests(WebApplicationFactory<Program> factory)
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
        public async Task GetPaymentAsync_ValidPaymentId_ReturnsPaymentDetails()
        {
            // Arrange
            // First, create a payment to retrieve later
            var postRequest = new PostPaymentRequest
            {
                CardNumber = "2222405343248877", // Authorized test card
                ExpiryMonth = 4,
                ExpiryYear = 2025,
                Currency = "GBP",
                Amount = 100,
                Cvv = "123"
            };

            var postResponse = await _client.PostAsJsonAsync("/api/payments", postRequest);
            postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var postResponseBody = await postResponse.Content.ReadFromJsonAsync<PostPaymentResponse>();
            postResponseBody.Should().NotBeNull();
            var paymentId = postResponseBody!.Id;

            // Act
            var getResponse = await _client.GetAsync($"/api/payments/{paymentId}");

            // Assert
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var getResponseBody = await getResponse.Content.ReadFromJsonAsync<GetPaymentResponse>();
            getResponseBody.Should().NotBeNull();
            getResponseBody!.Id.Should().Be(paymentId);
            getResponseBody.Status.Should().Be(PaymentStatus.Authorized.ToString());
            getResponseBody.CardNumberLastFour.Should().Be("8877");
            getResponseBody.ExpiryMonth.Should().Be(postRequest.ExpiryMonth);
            getResponseBody.ExpiryYear.Should().Be(postRequest.ExpiryYear);
            getResponseBody.Currency.Should().Be(postRequest.Currency);
            getResponseBody.Amount.Should().Be(postRequest.Amount);
        }

        [Fact]
        public async Task GetPaymentAsync_NonExistingPaymentId_ReturnsNotFound()
        {
            // Arrange
            var paymentId = Guid.NewGuid;

            // Act
            var getResponse = await _client.GetAsync($"/api/payments/{paymentId}");

            // Assert
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
