using PaymentGateway.Domain;
using PaymentGateway.Infrastructure.Models;

using System.Text;
using System.Text.Json;

namespace PaymentGateway.Infrastructure.Clients
{
    public class BankClient(HttpClient httpClient) : IBankClient
    {
        private readonly HttpClient _httpClient = httpClient;

        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<Result<BankResponse>> ProcessPaymentAsync(Payment payment)
        {
            var requestModel = new BankRequest
            {
                CardNumber = payment.CardNumber,
                ExpiryDate = $"{payment.ExpiryMonth:D2}/{payment.ExpiryYear}",
                Currency = payment.Currency,
                Amount = payment.Amount,
                Cvv = payment.Cvv
            };

            var jsonContent = JsonSerializer.Serialize(requestModel);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("/payments", httpContent);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Result<BankResponse>.Failure($"Bank returned status code {response.StatusCode}: {responseContent}");
                }

                var bankResponse = JsonSerializer.Deserialize<BankResponse>(responseContent, JsonSerializerOptions);

                return bankResponse == null
                    ? Result<BankResponse>.Failure("Failed to deserialize the bank response.")
                    : Result<BankResponse>.Success(bankResponse);
            }
            catch (Exception ex)
            {
                return Result<BankResponse>.Failure($"Unexpected bank error: {ex.Message}");
            }
        }
    }
}
