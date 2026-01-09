namespace ExcelComparer
{
    /// <summary>
    /// Configuration settings for Excel comparison operations.
    /// </summary>
    public class ComparisonConfig
    {
        /// <summary>
        /// Default tolerance for numeric comparisons.
        /// </summary>
        public const double DefaultTolerance = 1e-15;

        /// <summary>
        /// The tolerance value to use for numeric comparisons.
        /// </summary>
        public double Tolerance { get; set; } = DefaultTolerance;
    }
}
