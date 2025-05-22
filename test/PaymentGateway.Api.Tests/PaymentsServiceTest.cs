using FluentAssertions;

using Moq;

using PaymentGateway.Api.Utilities;
using PaymentGateway.Domain;
using PaymentGateway.Infrastructure.Clients;
using PaymentGateway.Infrastructure.Models;
using PaymentGateway.Infrastructure.Repositories;
using PaymentGateway.Services;

namespace PaymentGateway.Api.UnitTests
{
    public class PaymentsServiceTests
    {
        private readonly PaymentsService _sut;
        private readonly Mock<IPaymentsRepository> _paymentsRepositoryMock;
        private readonly Mock<IBankClient> _acquiringBankClientMock;

        public PaymentsServiceTests()
        {
            DateTimeProvider.SetUtcNow(new DateTime(2021, 1, 31, 15, 0, 0));
            _paymentsRepositoryMock = new Mock<IPaymentsRepository>();
            _acquiringBankClientMock = new Mock<IBankClient>();
            _sut = new PaymentsService(_paymentsRepositoryMock.Object, _acquiringBankClientMock.Object);
        }

        [Fact]
        public async Task ProcessPaymentAsync_ValidPayment_AuthorizesPayment()
        {
            // Arrange
            var payment = new Payment
            {
                CardNumber = "1234567890123456",
                ExpiryMonth = 12,
                ExpiryYear = DateTimeProvider.UtcNow.Year + 1,
                Currency = "USD",
                Amount = 1000,
                Cvv = "123"
            };

            var bankResponse = new BankResponse
            {
                Authorized = true
            };

            var successfulResult = Result<BankResponse>.Success(bankResponse);

            _acquiringBankClientMock
                .Setup(client => client.ProcessPaymentAsync(It.IsAny<Payment>()))
                .ReturnsAsync(successfulResult);

            _paymentsRepositoryMock
                .Setup(repo => repo.Add(It.IsAny<Payment>()))
                .Verifiable();

            // Act
            var result = await _sut.ProcessPaymentAsync(payment);

            // Assert
            result.Status.Should().Be(PaymentStatus.Authorized);
            result.Id.Should().NotBe(Guid.Empty);
            _acquiringBankClientMock.Verify(client => client.ProcessPaymentAsync(payment), Times.Once);
            _paymentsRepositoryMock.Verify(repo => repo.Add(payment), Times.Once);
        }

        [Fact]
        public async Task ProcessPaymentAsync_BankDeclinesPayment_SetsStatusToDeclined()
        {
            // Arrange
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                CardNumber = "1234567890123456",
                ExpiryMonth = 11,
                ExpiryYear = DateTimeProvider.UtcNow.Year + 1,
                Currency = "EUR",
                Amount = 500,
                Cvv = "456"
            };

            var bankResponse = new BankResponse
            {
                Authorized = false
            };

            var successfulResult = Result<BankResponse>.Success(bankResponse);

            _acquiringBankClientMock
                .Setup(client => client.ProcessPaymentAsync(It.IsAny<Payment>()))
                .ReturnsAsync(successfulResult);

            _paymentsRepositoryMock
                .Setup(repo => repo.Add(It.IsAny<Payment>()))
                .Verifiable();

            // Act
            var result = await _sut.ProcessPaymentAsync(payment);

            // Assert
            result.Status.Should().Be(PaymentStatus.Declined);
            _acquiringBankClientMock.Verify(client => client.ProcessPaymentAsync(payment), Times.Once);
            _paymentsRepositoryMock.Verify(repo => repo.Add(payment), Times.Once);
        }

        [Fact]
        public async Task ProcessPaymentAsync_BankError_SetsStatusToBankError()
        {
            // Arrange
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                CardNumber = "9876543210987654",
                ExpiryMonth = 10,
                ExpiryYear = DateTimeProvider.UtcNow.Year + 2,
                Currency = "GBP",
                Amount = 750,
                Cvv = "789"
            };

            var errorMessage = "Acquiring bank connection failed.";
            var failureResult = Result<BankResponse>.Failure(errorMessage);

            _acquiringBankClientMock
                .Setup(client => client.ProcessPaymentAsync(It.IsAny<Payment>()))
                .ReturnsAsync(failureResult);

            _paymentsRepositoryMock
                .Setup(repo => repo.Add(It.IsAny<Payment>()))
                .Verifiable();

            // Act
            var result = await _sut.ProcessPaymentAsync(payment);

            // Assert
            result.Status.Should().Be(PaymentStatus.BankError);
            _acquiringBankClientMock.Verify(client => client.ProcessPaymentAsync(payment), Times.Once);
            _paymentsRepositoryMock.Verify(repo => repo.Add(payment), Times.Once);
        }
    }
}
