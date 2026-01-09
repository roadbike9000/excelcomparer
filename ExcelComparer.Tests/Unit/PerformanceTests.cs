using ClosedXML.Excel;
using ExcelComparer.Utilities;
using FluentAssertions;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace ExcelComparer.Tests.Unit
{
    public class PerformanceTests : IDisposable
    {
        private readonly List<XLWorkbook> _workbooks = new();

        private IXLWorksheet CreateWorksheet(string name = "TestSheet")
        {
            var wb = new XLWorkbook();
            _workbooks.Add(wb);
            return wb.AddWorksheet(name);
        }

        [Fact]
        public void CompareWorksheets_BothEmpty_ShouldReturnImmediately()
        {
            // Arrange
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert
            result.TotalCellsCompared.Should().Be(0);
            result.TotalMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareWorksheets_SparseData_ShouldOnlyCompareUsedRange()
        {
            // Arrange - Create sparse worksheets with data only in corners
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            // Data only in cell A1 and Z100 (sparse)
            ws1.Cell(1, 1).Value = "A1";
            ws1.Cell(100, 26).Value = "Z100";

            ws2.Cell(1, 1).Value = "A1";
            ws2.Cell(100, 26).Value = "Z100";

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert - Should compare the rectangular range from A1 to Z100 (100 rows × 26 cols = 2600 cells)
            result.TotalCellsCompared.Should().Be(2600);
            result.TotalMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareWorksheets_OneEmptyOneWithSparseData_ShouldDetectMismatches()
        {
            // Arrange
            var ws1 = CreateWorksheet(); // Empty
            var ws2 = CreateWorksheet();

            ws2.Cell(50, 50).Value = "Data"; // Only one cell with data

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert - Should compare range from (1,1) to (50,50) = 50 rows × 50 cols = 2500 cells
            // This is the minimal bounding rectangle containing all data
            result.TotalCellsCompared.Should().Be(2500);
            result.TextMismatches.Should().Be(1); // Only the one non-empty cell differs
        }

        [Fact]
        public void CompareWorksheets_NonOverlappingData_ShouldCompareUnion()
        {
            // Arrange
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            ws1.Cell(1, 1).Value = "TopLeft";
            ws2.Cell(10, 10).Value = "BottomRight";

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert - Should compare the rectangular union: 10 rows × 10 cols = 100 cells
            result.TotalCellsCompared.Should().Be(100);
            result.TotalMismatches.Should().Be(2); // Both cells differ (one empty vs filled in each)
        }

        [Fact]
        public void CompareWorksheets_PerformanceComparison_SparseVsDense()
        {
            // Arrange - Create two identical sparse worksheets
            var ws1Sparse = CreateWorksheet();
            var ws2Sparse = CreateWorksheet();

            // Add data only in 4 corners of a large range (sparse)
            ws1Sparse.Cell(1, 1).Value = "A1";
            ws1Sparse.Cell(1, 100).Value = "CV1";
            ws1Sparse.Cell(1000, 1).Value = "A1000";
            ws1Sparse.Cell(1000, 100).Value = "CV1000";

            ws2Sparse.Cell(1, 1).Value = "A1";
            ws2Sparse.Cell(1, 100).Value = "CV1";
            ws2Sparse.Cell(1000, 1).Value = "A1000";
            ws2Sparse.Cell(1000, 100).Value = "CV1000";

            var output = new StringWriter();
            Console.SetOut(output);

            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = CellComparer.CompareWorksheets(ws1Sparse, ws2Sparse, "TestSheet", 1e-15);

            stopwatch.Stop();

            // Assert
            result.TotalCellsCompared.Should().Be(100000); // 1000 rows × 100 cols
            result.TotalMismatches.Should().Be(0);

            // With optimization, this should complete quickly even for 100k cells
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should be much faster, but set conservative limit
        }

        [Fact]
        public void CompareWorksheets_DataStartsAtNonOrigin_ShouldHandleCorrectly()
        {
            // Arrange - Data doesn't start at A1
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            ws1.Cell(10, 10).Value = "Start";
            ws1.Cell(20, 20).Value = "End";

            ws2.Cell(10, 10).Value = "Start";
            ws2.Cell(20, 20).Value = "End";

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert - Should compare from (1,1) to (20,20) = 20 rows × 20 cols = 400 cells
            // Optimization: Compares minimal bounding rectangle, not entire possible sheet
            result.TotalCellsCompared.Should().Be(400);
            result.TotalMismatches.Should().Be(0);
        }

        public void Dispose()
        {
            foreach (var wb in _workbooks)
            {
                wb.Dispose();
            }
        }
    }
}
