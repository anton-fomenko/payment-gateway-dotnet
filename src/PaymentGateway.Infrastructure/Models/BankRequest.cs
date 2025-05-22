using System.Text.Json.Serialization;

namespace PaymentGateway.Infrastructure.Models
{
    internal class BankRequest
    {
        [JsonPropertyName("card_number")]
        public required string CardNumber { get; set; }

        [JsonPropertyName("expiry_date")]
        public required string ExpiryDate { get; set; } // Format: "MM/yyyy"

        [JsonPropertyName("currency")]
        public required string Currency { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("cvv")]
        public required string Cvv { get; set; }
    }
}
