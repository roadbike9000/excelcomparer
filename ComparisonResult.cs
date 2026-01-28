namespace ExcelComparer
{
    /// <summary>
    /// Tracks statistics and results from an Excel comparison operation.
    /// </summary>
    public class ComparisonResult
    {
        public long TotalCellsCompared { get; set; }
        public int NumericMismatches { get; set; }
        public int TextMismatches { get; set; }
        public int SheetsCompared { get; set; }

        /// <summary>
        /// Indicates whether the comparison completed successfully.
        /// False indicates a fatal error occurred (e.g., file read failure).
        /// </summary>
        public bool Success { get; set; } = true;

        public int TotalMismatches => NumericMismatches + TextMismatches;

        public void PrintSummary()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== Comparison Summary ===");
            Console.ResetColor();

            if (!Success)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ Comparison failed - unable to complete");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"Sheets compared: {SheetsCompared}");
            Console.WriteLine($"Total cells compared: {TotalCellsCompared}");
            Console.WriteLine($"Numeric mismatches: {NumericMismatches}");
            Console.WriteLine($"Text mismatches: {TextMismatches}");

            if (TotalMismatches == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ No differences found - files are identical!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Total differences found: {TotalMismatches}");
            }
            Console.ResetColor();
        }
    }
}
