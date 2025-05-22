namespace PaymentGateway.Api.Utilities
{
    /// <summary>
    /// Wrapper around DateTime.UtcNow to allow for setting a specific date and time for testing purposes.
    /// </summary>
    public static class DateTimeProvider
    {
        private static DateTime? _utcNow;

        /// <summary>
        /// Gets the current UTC date and time. Returns the set value if overridden.
        /// </summary>
        public static DateTime UtcNow => _utcNow ?? DateTime.UtcNow;

        /// <summary>
        /// Sets the current UTC date and time. Use null to reset to system time.
        /// </summary>
        /// <param name="date">The date and time to set.</param>
        public static void SetUtcNow(DateTime? date)
        {
            _utcNow = date;
        }

        /// <summary>
        /// Resets the overridden date to the system's current UTC date and time.
        /// </summary>
        public static void Reset()
        {
            _utcNow = null;
        }
    }
}
