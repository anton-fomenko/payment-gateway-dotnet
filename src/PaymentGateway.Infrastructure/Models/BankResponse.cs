namespace PaymentGateway.Infrastructure.Models
{
    public class BankResponse
    {
        public bool Authorized { get; set; }
        public string? AuthorizationCode { get; set; }
    }
}
