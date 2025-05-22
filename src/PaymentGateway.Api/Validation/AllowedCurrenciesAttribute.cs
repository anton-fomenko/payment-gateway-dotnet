using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.Validation
{
    public class AllowedCurrenciesAttribute(string[] allowedCurrencies) : ValidationAttribute
    {
        private readonly string[] _allowedCurrencies = allowedCurrencies;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var currency = value as string;

            return !_allowedCurrencies.Contains(currency)
                ? new ValidationResult($"Currency must be one of the following: {string.Join(", ", _allowedCurrencies)}.")
                : ValidationResult.Success;
        }
    }
}
