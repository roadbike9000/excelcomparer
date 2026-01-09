using ClosedXML.Excel;
using ExcelComparer.Utilities;
using System;

namespace ExcelComparer.Modes
{
    public static class CompareSingleSheet
    {
        public static ComparisonResult Run(string file1, string file2, string sheetName, double tolerance)
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

            if (!wb1.Worksheets.Contains(sheetName) || !wb2.Worksheets.Contains(sheetName))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Sheet '{sheetName}' not found in one or both workbooks.");
                Console.ResetColor();
                return new ComparisonResult();
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Comparing sheet: {sheetName}");
            Console.ResetColor();

            var ws1 = wb1.Worksheet(sheetName);
            var ws2 = wb2.Worksheet(sheetName);

            return CellComparer.CompareWorksheets(ws1, ws2, sheetName, tolerance);
            }
        }
    }
}
