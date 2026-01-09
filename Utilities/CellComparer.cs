using ClosedXML.Excel;
using System;

namespace ExcelComparer.Utilities
{
    /// <summary>
    /// Utility class for comparing Excel cells and worksheets.
    /// </summary>
    public static class CellComparer
    {
        /// <summary>
        /// Compares two worksheets and reports differences.
        /// </summary>
        public static ComparisonResult CompareWorksheets(IXLWorksheet ws1, IXLWorksheet ws2, string sheetName, double tolerance)
        {
            var result = new ComparisonResult { SheetsCompared = 1 };

            // Get used ranges for performance optimization
            var usedRange1 = ws1.RangeUsed();
            var usedRange2 = ws2.RangeUsed();

            // If both sheets are empty, return immediately
            if (usedRange1 == null && usedRange2 == null)
            {
                return result;
            }

            // Determine the bounds to compare (union of both used ranges)
            int minRow = 1;
            int maxRow = 0;
            int minCol = 1;
            int maxCol = 0;

            if (usedRange1 != null)
            {
                minRow = Math.Min(minRow, usedRange1.FirstRow().RowNumber());
                maxRow = Math.Max(maxRow, usedRange1.LastRow().RowNumber());
                minCol = Math.Min(minCol, usedRange1.FirstColumn().ColumnNumber());
                maxCol = Math.Max(maxCol, usedRange1.LastColumn().ColumnNumber());
            }

            if (usedRange2 != null)
            {
                minRow = usedRange2.FirstRow().RowNumber() < minRow ? usedRange2.FirstRow().RowNumber() : minRow;
                maxRow = Math.Max(maxRow, usedRange2.LastRow().RowNumber());
                minCol = usedRange2.FirstColumn().ColumnNumber() < minCol ? usedRange2.FirstColumn().ColumnNumber() : minCol;
                maxCol = Math.Max(maxCol, usedRange2.LastColumn().ColumnNumber());
            }

            // Handle edge case where one sheet is empty
            if (maxRow == 0 || maxCol == 0)
            {
                // One sheet is empty, the other is not - all non-empty cells are mismatches
                var nonEmptyRange = usedRange1 ?? usedRange2;
                if (nonEmptyRange != null)
                {
                    maxRow = nonEmptyRange.LastRow().RowNumber();
                    maxCol = nonEmptyRange.LastColumn().ColumnNumber();
                    minRow = nonEmptyRange.FirstRow().RowNumber();
                    minCol = nonEmptyRange.FirstColumn().ColumnNumber();
                }
            }

            for (int row = minRow; row <= maxRow; row++)
            {
                for (int col = minCol; col <= maxCol; col++)
                {
                    result.TotalCellsCompared++;

                    var c1 = ws1.Cell(row, col);
                    var c2 = ws2.Cell(row, col);

                    bool isNum1 = c1.TryGetValue(out double d1);
                    bool isNum2 = c2.TryGetValue(out double d2);

                    if (isNum1 && isNum2)
                    {
                        if (Math.Abs(d1 - d2) > tolerance)
                        {
                            result.NumericMismatches++;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Numeric mismatch at {sheetName}!R{row}C{col}: {d1} vs {d2}");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        var s1 = c1.Value.ToString() ?? "";
                        var s2 = c2.Value.ToString() ?? "";

                        if (!string.Equals(s1, s2, StringComparison.Ordinal))
                        {
                            result.TextMismatches++;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Text mismatch at {sheetName}!R{row}C{col}: '{s1}' vs '{s2}'");
                            Console.ResetColor();
                        }
                    }
                }
            }

            return result;
        }
    }
}
