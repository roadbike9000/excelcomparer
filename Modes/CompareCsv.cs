using ExcelComparer.Utilities;
using System;
using System.IO;

namespace ExcelComparer.Modes
{
    /// <summary>
    /// Mode for comparing CSV files.
    /// </summary>
    public static class CompareCsv
    {
        /// <summary>
        /// Compares two CSV files.
        /// </summary>
        public static ComparisonResult Run(string file1, string file2, double tolerance)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Comparing CSV files...");
            Console.ResetColor();

            // Get file names for display
            string name1 = Path.GetFileName(file1);
            string name2 = Path.GetFileName(file2);
            string identifier = $"{name1} vs {name2}";

            return CsvComparer.CompareCsvFiles(file1, file2, identifier, tolerance);
        }
    }
}
