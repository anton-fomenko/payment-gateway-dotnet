using PaymentGateway.Domain;
using PaymentGateway.Infrastructure.Clients;
using PaymentGateway.Infrastructure.Repositories;

namespace PaymentGateway.Services
{
    public class PaymentsService(IPaymentsRepository repository, IBankClient acquiringBankClient) : IPaymentsService
    {
        private readonly IPaymentsRepository _repository = repository;
        private readonly IBankClient _acquiringBankClient = acquiringBankClient;

        public async Task<Payment> ProcessPaymentAsync(Payment payment)
        {
            var bankResult = await _acquiringBankClient.ProcessPaymentAsync(payment);

            payment.Id = Guid.NewGuid();

            if (bankResult.IsSuccess)
            {
                ArgumentNullException.ThrowIfNull(bankResult.Value);

                payment.Status = bankResult.Value.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined;
                payment.AuthorizationCode = bankResult.Value.AuthorizationCode;
            }
            else
            {
                payment.Status = PaymentStatus.BankError;
            }

            _repository.Add(payment);

            return payment;
        }

        // Since our repository is working synchronously for now, async/await here is not necessary.
        // Nevertheless, I specified async/await here so that we can easily switch to an async repository in the future.
        public async Task<Payment?> GetPaymentAsync(Guid id)
        {
            return await Task.FromResult(_repository.Get(id));
        }
    }
}
