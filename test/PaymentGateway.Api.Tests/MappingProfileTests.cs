using AutoMapper;

using FluentAssertions;

using PaymentGateway.Api.Mappings;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Domain;

namespace PaymentGateway.Api.UnitTests
{
    public class MappingProfileTests
    {
        private readonly IMapper _mapper;

        public MappingProfileTests()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());

            _mapper = config.CreateMapper();
        }

        #region Payment to GetPaymentResponse Mapping Tests

        [Fact]
        public void PaymentToGetPaymentResponse_Mapping_ShouldMapCorrectly()
        {
            // Arrange
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                Status = PaymentStatus.Authorized,
                CardNumber = "1234567812345678",
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                Currency = "USD",
                Amount = 1000,
                Cvv = "123"
            };

            var expectedLastFour = "5678";

            // Act
            var response = _mapper.Map<GetPaymentResponse>(payment);

            // Assert
            response.Should().NotBeNull();
            response.CardNumberLastFour.Should().Be(expectedLastFour);
            response.Id.Should().Be(payment.Id);
            response.Status.Should().Be(payment.Status.ToString());
            response.ExpiryMonth.Should().Be(payment.ExpiryMonth);
            response.ExpiryYear.Should().Be(payment.ExpiryYear);
            response.Currency.Should().Be(payment.Currency);
            response.Amount.Should().Be(payment.Amount);
        }

        [Theory]
        [InlineData("1111222233334444", "4444")]
        [InlineData("5555666677778888", "8888")]
        [InlineData("9999000011112222", "2222")]
        public void PaymentToGetPaymentResponse_Mapping_LastFourDigits_VariousCases(string cardNumber, string expectedLastFour)
        {
            // Arrange
            var payment = new Payment
            {
                CardNumber = cardNumber,
                Currency = "USD",
                Cvv = "123"
            };

            // Act
            var response = _mapper.Map<GetPaymentResponse>(payment);

            // Assert
            response.CardNumberLastFour.Should().Be(expectedLastFour);
        }

        [Fact]
        public void PaymentToGetPaymentResponse_Mapping_WithShortCardNumber_Should_ThrowException()
        {
            // Arrange
            var payment = new Payment
            {
                CardNumber = "123", // Less than 4 digits
                Currency = "USD",
                Cvv = "123"
            };

            // Act
            Action act = () => _mapper.Map<GetPaymentResponse>(payment);

            // Assert
            act.Should().Throw<AutoMapperMappingException>();
        }

        #endregion

        #region Payment to PostPaymentResponse Mapping Tests

        [Theory]
        [InlineData("3333444455556666", "6666")]
        [InlineData("7777888899990000", "0000")]
        [InlineData("1212121212121212", "1212")]
        public void PaymentToPostPaymentResponse_Mapping_ProducesOnlyLastFourDigits(string cardNumber, string expectedLastFour)
        {
            // Arrange
            var payment = new Payment
            {
                CardNumber = cardNumber,
                Currency = "USD",
                Cvv = "123"
            };

            // Act
            var response = _mapper.Map<PostPaymentResponse>(payment);

            // Assert
            response.CardNumberLastFour.Should().Be(expectedLastFour);
        }

        [Fact]
        public void PaymentToPostPaymentResponse_Mapping_WithShortCardNumber_Should_ThrowException()
        {
            // Arrange
            var payment = new Payment
            {
                CardNumber = "999", // Less than 4 digits
                Currency = "USD",
                Cvv = "123"
            };

            // Act
            Action act = () => _mapper.Map<PostPaymentResponse>(payment);

            // Assert
            act.Should().Throw<AutoMapperMappingException>();
        }

        #endregion

        #region PostPaymentRequest to Payment Mapping Tests

        [Fact]
        public void PostPaymentRequestToPayment_Mapping_Should_Map_All_Properties_Correctly()
        {
            // Arrange
            var request = new PostPaymentRequest
            {
                CardNumber = "1234567890123456",
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                Currency = "USD",
                Amount = 1000,
                Cvv = "123"
            };

            // Act
            var payment = _mapper.Map<Payment>(request);

            // Assert
            payment.Should().NotBeNull();
            payment.CardNumber.Should().Be(request.CardNumber);
            payment.ExpiryMonth.Should().Be(request.ExpiryMonth);
            payment.ExpiryYear.Should().Be(request.ExpiryYear);
            payment.Currency.Should().Be(request.Currency);
            payment.Amount.Should().Be(request.Amount);
            payment.Cvv.Should().Be(request.Cvv);
        }

        #endregion
    }
}
