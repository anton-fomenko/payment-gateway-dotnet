using System.ComponentModel.DataAnnotations;

using PaymentGateway.Api.Utilities;
using PaymentGateway.Api.Validation;

namespace PaymentGateway.Api.Models.Requests
{
    public class PostPaymentRequest : IValidatableObject
    {
        [Required]
        [RegularExpression(@"^\d{14,19}$", ErrorMessage = "Card number must be between 14 and 19 digits.")]
        public string? CardNumber { get; set; }

        [Required]
        [Range(1, 12, ErrorMessage = "Expiry month must be between 1 and 12.")]
        public int ExpiryMonth { get; set; }

        [Required]
        [Range(typeof(int), "1", "9998", ErrorMessage = "Expiry year must be a valid year in the future.")]
        public int ExpiryYear { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be 3 characters long.")]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Currency must be a valid ISO currency code.")]
        [AllowedCurrencies(["USD", "EUR", "GBP"])]
        public string? Currency { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Amount must be a positive integer.")]
        public int Amount { get; set; }

        [Required]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must be 3 or 4 digits.")]
        public string? Cvv { get; set; }

        // Validate that expiry date is in the future.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            var currentDate = DateTimeProvider.UtcNow;
            // Example: Suppose today is December 31, 2024, 15:30 UTC

            DateTime firstDayOfExpiryMonth = new(ExpiryYear, ExpiryMonth, 1);
            // Example: December 1, 2024

            DateTime firstDayOfNextMonth = firstDayOfExpiryMonth.AddMonths(1);
            // Example: December 1, 2024 + 1 month = January 1, 2025

            // Validate that the current date (December 31, 2024, 15:30 UTC) is before the first day of the next month (January 1, 2025)
            if (currentDate >= firstDayOfNextMonth)
            {
                results.Add(new ValidationResult(
                    "The card expiry date must be in the future.",
                    [nameof(ExpiryMonth), nameof(ExpiryYear)]));
            }

            return results;
        }
    }
}