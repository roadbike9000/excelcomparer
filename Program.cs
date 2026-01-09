/// <summary>
/// The entry point for the ExcelComparer application.
/// Compares two Excel files either across all sheets or a single specified sheet.
/// </summary>
/// <param name="args">
/// Command-line arguments:
/// <list type="bullet">
/// <item><description><c>file1</c>: Path to the first Excel file (required)</description></item>
/// <item><description><c>file2</c>: Path to the second Excel file (required)</description></item>
/// <item><description><c>--all</c>: Compares all common sheets in the two Excel files</description></item>
/// <item><description><c>--sheet [name]</c>: Compares a specific sheet (defaults to "Sheet1")</description></item>
/// <item><description><c>--tolerance [value]</c>: Numeric tolerance for comparisons (default: 1e-15)</description></item>
/// </list>
/// </param>
/// Usage:
/// dotnet build
/// dotnet run -- file1.xlsx file2.xlsx --all
/// dotnet run -- file1.xlsx file2.xlsx --sheet "SheetName"
/// dotnet run -- file1.xlsx file2.xlsx --all --tolerance 1e-10
using System;
using System.Globalization;
using ExcelComparer.Modes;
using ExcelComparer.Utilities;

namespace ExcelComparer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                PrintUsage();
                return;
            }

            string file1 = args[0];
            string file2 = args[1];

            // Parse command-line options
            var config = new ComparisonConfig();
            bool compareAll = false;
            string? sheetName = null;

            for (int i = 2; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--all":
                        compareAll = true;
                        break;

                    case "--sheet":
                        if (compareAll)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Warning: --sheet is ignored when --all is specified");
                            Console.ResetColor();
                        }
                        if (i + 1 < args.Length)
                        {
                            sheetName = args[++i];
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Error: --sheet requires a sheet name argument");
                            Console.ResetColor();
                            return;
                        }
                        break;

                    case "--tolerance":
                        if (i + 1 < args.Length)
                        {
                            if (double.TryParse(args[++i], NumberStyles.Float, CultureInfo.InvariantCulture, out double tolerance))
                            {
                                if (tolerance < 0)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Error: Tolerance must be non-negative");
                                    Console.ResetColor();
                                    return;
                                }
                                if (double.IsNaN(tolerance) || double.IsInfinity(tolerance))
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Error: Tolerance must be a valid finite number");
                                    Console.ResetColor();
                                    return;
                                }
                                config.Tolerance = tolerance;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Error: Invalid tolerance value '{args[i]}'");
                                Console.ResetColor();
                                return;
                            }
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Error: --tolerance requires a numeric value");
                            Console.ResetColor();
                            return;
                        }
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Warning: Unknown option '{args[i]}'");
                        Console.ResetColor();
                        break;
                }
            }

            // Validate files exist
            if (!File.Exists(file1))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: File not found: {file1}");
                Console.ResetColor();
                return;
            }

            if (!File.Exists(file2))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: File not found: {file2}");
                Console.ResetColor();
                return;
            }

            // Detect file types
            if (!FileTypeDetector.AreSameType(file1, file2))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Both files must be the same type (both Excel or both CSV)");
                Console.WriteLine($"  File 1: {FileTypeDetector.DetectFileType(file1)}");
                Console.WriteLine($"  File 2: {FileTypeDetector.DetectFileType(file2)}");
                Console.ResetColor();
                return;
            }

            var fileType = FileTypeDetector.DetectFileType(file1);
            if (fileType == FileTypeDetector.FileType.Unknown)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Unsupported file type. Supported types: .xlsx, .xlsm, .xlsb, .xls, .csv, .txt");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"Comparing files:");
            Console.WriteLine($"  File 1: {file1}");
            Console.WriteLine($"  File 2: {file2}");
            Console.WriteLine($"  File Type: {fileType}");
            Console.WriteLine($"  Tolerance: {config.Tolerance}");
            Console.WriteLine();

            ComparisonResult result;

            try
            {
                if (fileType == FileTypeDetector.FileType.Csv)
                {
                    // CSV files don't have sheets, so --all and --sheet are ignored
                    if (compareAll)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Note: --all flag is ignored for CSV files (CSV files have no sheets)");
                        Console.ResetColor();
                    }
                    if (sheetName != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Note: --sheet flag is ignored for CSV files (CSV files have no sheets)");
                        Console.ResetColor();
                    }

                    result = CompareCsv.Run(file1, file2, config.Tolerance);
                }
                else // Excel files
                {
                    if (compareAll)
                    {
                        result = CompareAllSheets.Run(file1, file2, config.Tolerance);
                    }
                    else
                    {
                        string sheet = sheetName ?? "Sheet1";
                        result = CompareSingleSheet.Run(file1, file2, sheet, config.Tolerance);
                    }
                }

                result.PrintSummary();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error during comparison: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("ExcelComparer - Compare two Excel files");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run -- <file1> <file2> [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --all                  Compare all common sheets");
            Console.WriteLine("  --sheet <name>         Compare a specific sheet (default: Sheet1)");
            Console.WriteLine("  --tolerance <value>    Numeric comparison tolerance (default: 1e-15)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run -- file1.xlsx file2.xlsx --all");
            Console.WriteLine("  dotnet run -- file1.xlsx file2.xlsx --sheet \"Sheet1\"");
            Console.WriteLine("  dotnet run -- file1.xlsx file2.xlsx --all --tolerance 1e-10");
        }
    }
}
