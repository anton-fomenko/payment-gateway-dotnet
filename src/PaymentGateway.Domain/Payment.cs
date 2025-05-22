namespace PaymentGateway.Domain
{
    public class Payment
    {
        public Guid Id { get; set; }
        public PaymentStatus Status { get; set; }
        public required string CardNumber { get; set; } // Handle securely
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public required string Currency { get; set; }
        public int Amount { get; set; }
        public required string Cvv { get; set; } // Handle securely
        public string? AuthorizationCode { get; set; } // Handle securely
    }
}
