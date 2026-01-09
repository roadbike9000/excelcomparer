using ClosedXML.Excel;
using ExcelComparer.Utilities;
using System;
using System.Linq;

namespace ExcelComparer.Modes
{
    public static class CompareAllSheets
    {
        public static ComparisonResult Run(string file1, string file2, double tolerance)
        {
            XLWorkbook wb1, wb2;

            try
            {
                wb1 = new XLWorkbook(file1);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is IOException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Cannot open '{file1}' - {ex.Message}");
                Console.ResetColor();
                return new ComparisonResult();
            }

            try
            {
                wb2 = new XLWorkbook(file2);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is IOException)
            {
                wb1.Dispose();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Cannot open '{file2}' - {ex.Message}");
                Console.ResetColor();
                return new ComparisonResult();
            }

            using (wb1)
            using (wb2)
            {

            var commonSheetNames = wb1.Worksheets.Select(w => w.Name)
                .Intersect(wb2.Worksheets.Select(w => w.Name))
                .ToList();

            var overallResult = new ComparisonResult();

            foreach (var sheet in commonSheetNames)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Comparing sheet: {sheet}");
                Console.ResetColor();

                var ws1 = wb1.Worksheet(sheet);
                var ws2 = wb2.Worksheet(sheet);

                var sheetResult = CellComparer.CompareWorksheets(ws1, ws2, sheet, tolerance);

                // Aggregate results
                overallResult.TotalCellsCompared += sheetResult.TotalCellsCompared;
                overallResult.NumericMismatches += sheetResult.NumericMismatches;
                overallResult.TextMismatches += sheetResult.TextMismatches;
                overallResult.SheetsCompared++;
            }

            return overallResult;
            }
        }
    }
}
