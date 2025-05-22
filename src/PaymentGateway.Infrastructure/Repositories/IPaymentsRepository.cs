using PaymentGateway.Domain;

namespace PaymentGateway.Infrastructure.Repositories
{
    public interface IPaymentsRepository
    {
        void Add(Payment payment);

        Payment? Get(Guid id);
    }
}
