using PaymentGateway.Domain;
using PaymentGateway.Infrastructure.Models;

namespace PaymentGateway.Infrastructure.Clients
{
    public interface IBankClient
    {
        Task<Result<BankResponse>> ProcessPaymentAsync(Payment payment);
    }
}
