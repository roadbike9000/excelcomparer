using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ExcelComparer.Utilities
{
    /// <summary>
    /// Utility class for comparing CSV files.
    /// </summary>
    public static class CsvComparer
    {
        private const int MaxReportedDifferences = 100;

        /// <summary>
        /// Compares two CSV files and reports differences.
        /// </summary>
        public static ComparisonResult CompareCsvFiles(string file1, string file2, string identifier, double tolerance)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(file1))
                throw new ArgumentException("File path cannot be null or empty", nameof(file1));
            if (string.IsNullOrWhiteSpace(file2))
                throw new ArgumentException("File path cannot be null or empty", nameof(file2));
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("Identifier cannot be null or empty", nameof(identifier));
            if (tolerance < 0 || double.IsNaN(tolerance) || double.IsInfinity(tolerance))
                throw new ArgumentException("Tolerance must be a non-negative finite number", nameof(tolerance));

            List<List<string>> data1, data2;

            try
            {
                data1 = ReadCsvFile(file1);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Cannot read '{file1}' - {ex.Message}");
                Console.ResetColor();
                return new ComparisonResult { SheetsCompared = 0 }; // No comparison performed
            }

            try
            {
                data2 = ReadCsvFile(file2);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Cannot read '{file2}' - {ex.Message}");
                Console.ResetColor();
                return new ComparisonResult { SheetsCompared = 0 }; // No comparison performed
            }

            var result = new ComparisonResult { SheetsCompared = 1 };
            int reportedDifferences = 0;

            // Compare row by row
            int maxRows = Math.Max(data1.Count, data2.Count);

            for (int row = 0; row < maxRows; row++)
            {
                var row1 = row < data1.Count ? data1[row] : new List<string>();
                var row2 = row < data2.Count ? data2[row] : new List<string>();

                int maxCols = Math.Max(row1.Count, row2.Count);

                for (int col = 0; col < maxCols; col++)
                {
                    result.TotalCellsCompared++;

                    var value1 = col < row1.Count ? row1[col] : "";
                    var value2 = col < row2.Count ? row2[col] : "";

                    // Try numeric comparison first (parse once and cache results)
                    bool isNum1 = double.TryParse(value1, NumberStyles.Float, CultureInfo.InvariantCulture, out double d1);
                    bool isNum2 = double.TryParse(value2, NumberStyles.Float, CultureInfo.InvariantCulture, out double d2);

                    if (isNum1 && isNum2)
                    {
                        // Both are numbers - compare numerically
                        if (Math.Abs(d1 - d2) > tolerance)
                        {
                            result.NumericMismatches++;

                            if (reportedDifferences < MaxReportedDifferences)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Numeric mismatch at {identifier}!R{row + 1}C{col + 1}: {d1} vs {d2}");
                                Console.ResetColor();
                                reportedDifferences++;
                            }
                            else if (reportedDifferences == MaxReportedDifferences)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"... suppressing further difference output (max {MaxReportedDifferences} shown)");
                                Console.ResetColor();
                                reportedDifferences++;
                            }
                        }
                    }
                    else if (isNum1 != isNum2)
                    {
                        // One is number, one is not - type mismatch
                        result.TextMismatches++;

                        if (reportedDifferences < MaxReportedDifferences)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Type mismatch at {identifier}!R{row + 1}C{col + 1}: '{value1}' vs '{value2}'");
                            Console.ResetColor();
                            reportedDifferences++;
                        }
                        else if (reportedDifferences == MaxReportedDifferences)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"... suppressing further difference output (max {MaxReportedDifferences} shown)");
                            Console.ResetColor();
                            reportedDifferences++;
                        }
                    }
                    else
                    {
                        // Both are text - compare as strings
                        if (!string.Equals(value1, value2, StringComparison.Ordinal))
                        {
                            result.TextMismatches++;

                            if (reportedDifferences < MaxReportedDifferences)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Text mismatch at {identifier}!R{row + 1}C{col + 1}: '{value1}' vs '{value2}'");
                                Console.ResetColor();
                                reportedDifferences++;
                            }
                            else if (reportedDifferences == MaxReportedDifferences)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"... suppressing further difference output (max {MaxReportedDifferences} shown)");
                                Console.ResetColor();
                                reportedDifferences++;
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Reads a CSV file into a list of rows (each row is a list of values).
        /// Auto-detects delimiter and handles various CSV formats.
        /// </summary>
        private static List<List<string>> ReadCsvFile(string filePath)
        {
            const long MAX_FILE_SIZE = 100 * 1024 * 1024; // 100 MB limit
            const int MAX_ROWS = 1_000_000; // 1 million rows limit

            var rows = new List<List<string>>();

            // Validate file size to prevent DoS
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > MAX_FILE_SIZE)
            {
                throw new InvalidOperationException(
                    $"File size ({fileInfo.Length:N0} bytes) exceeds maximum allowed size ({MAX_FILE_SIZE:N0} bytes)");
            }

            // Try to detect encoding, fall back to UTF-8 if detection fails
            Encoding encoding;
            try
            {
                encoding = DetectEncoding(filePath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Log warning but continue with default encoding
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: Could not detect encoding for '{Path.GetFileName(filePath)}', using UTF-8. Error: {ex.Message}");
                Console.ResetColor();
                encoding = Encoding.UTF8;
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false, // Treat all rows as data
                MissingFieldFound = null, // Don't throw on missing fields
                BadDataFound = null, // Don't throw on bad data
                DetectDelimiter = true, // Auto-detect delimiter
                TrimOptions = TrimOptions.None // Preserve whitespace
            };

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(fileStream, encoding);
            using var csv = new CsvReader(reader, config);

            while (csv.Read())
            {
                if (rows.Count >= MAX_ROWS)
                {
                    throw new InvalidOperationException(
                        $"File contains more than {MAX_ROWS:N0} rows, which exceeds the maximum allowed");
                }

                var row = new List<string>();
                for (int i = 0; csv.TryGetField<string>(i, out var value); i++)
                {
                    row.Add(value ?? "");
                }
                rows.Add(row);
            }

            return rows;
        }

        /// <summary>
        /// Detects file encoding by checking for Byte Order Mark (BOM) and common patterns.
        /// Supports UTF-8, UTF-16 (BE/LE), and UTF-32 (BE/LE) encodings.
        /// </summary>
        /// <param name="filePath">Path to the file to analyze</param>
        /// <returns>Detected encoding, defaults to UTF-8 without BOM if no BOM found</returns>
        /// <exception cref="IOException">Thrown when file cannot be accessed</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when access is denied</exception>
        private static Encoding DetectEncoding(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // Check for BOM
            byte[] bom = new byte[4];
            int bytesRead = stream.Read(bom, 0, 4);

            // UTF-8 BOM
            if (bytesRead >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                return new UTF8Encoding(true);

            // UTF-16 BE BOM
            if (bytesRead >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)
                return new UnicodeEncoding(true, true);

            // UTF-16 LE BOM
            if (bytesRead >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
                return new UnicodeEncoding(false, true);

            // UTF-32 BE BOM
            if (bytesRead >= 4 && bom[0] == 0x00 && bom[1] == 0x00 && bom[2] == 0xFE && bom[3] == 0xFF)
                return new UTF32Encoding(true, true);

            // UTF-32 LE BOM
            if (bytesRead >= 4 && bom[0] == 0xFF && bom[1] == 0xFE && bom[2] == 0x00 && bom[3] == 0x00)
                return new UTF32Encoding(false, true);

            // Default to UTF-8 without BOM
            return new UTF8Encoding(false);
        }
    }
}
