using PaymentGateway.Domain;

namespace PaymentGateway.Services
{
    public interface IPaymentsService
    {
        Task<Payment> ProcessPaymentAsync(Payment payment);
        Task<Payment?> GetPaymentAsync(Guid id);
    }
}
