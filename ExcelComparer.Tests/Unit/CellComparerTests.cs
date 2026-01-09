using ClosedXML.Excel;
using ExcelComparer.Utilities;
using FluentAssertions;
using System.IO;
using Xunit;

namespace ExcelComparer.Tests.Unit
{
    public class CellComparerTests : IDisposable
    {
        private readonly List<XLWorkbook> _workbooks = new();

        private IXLWorksheet CreateWorksheet(string name = "TestSheet")
        {
            var wb = new XLWorkbook();
            _workbooks.Add(wb);
            return wb.AddWorksheet(name);
        }

        [Fact]
        public void CompareWorksheets_IdenticalNumericCells_ShouldReturnNoMismatches()
        {
            // Arrange
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            ws1.Cell(1, 1).Value = 123.456;
            ws2.Cell(1, 1).Value = 123.456;

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert
            result.TotalCellsCompared.Should().Be(1);
            result.NumericMismatches.Should().Be(0);
            result.TextMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareWorksheets_NumericDifferenceWithinTolerance_ShouldReturnNoMismatches()
        {
            // Arrange
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            ws1.Cell(1, 1).Value = 1.0;
            ws2.Cell(1, 1).Value = 1.0 + 1e-16; // Within default tolerance

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert
            result.NumericMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareWorksheets_NumericDifferenceBeyondTolerance_ShouldReturnMismatch()
        {
            // Arrange
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            ws1.Cell(1, 1).Value = 1.0;
            ws2.Cell(1, 1).Value = 1.001;

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert
            result.NumericMismatches.Should().Be(1);
            result.TextMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareWorksheets_IdenticalTextCells_ShouldReturnNoMismatches()
        {
            // Arrange
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            ws1.Cell(1, 1).Value = "Hello World";
            ws2.Cell(1, 1).Value = "Hello World";

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert
            result.TextMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareWorksheets_DifferentTextCells_ShouldReturnMismatch()
        {
            // Arrange
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            ws1.Cell(1, 1).Value = "Hello";
            ws2.Cell(1, 1).Value = "World";

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert
            result.TextMismatches.Should().Be(1);
        }

        [Fact]
        public void CompareWorksheets_CaseSensitiveText_ShouldReturnMismatch()
        {
            // Arrange
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            ws1.Cell(1, 1).Value = "hello";
            ws2.Cell(1, 1).Value = "HELLO";

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert
            result.TextMismatches.Should().Be(1);
        }

        [Fact]
        public void CompareWorksheets_EmptyCells_ShouldMatch()
        {
            // Arrange
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert
            result.TotalMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareWorksheets_OneEmptyOneWithValue_ShouldReturnMismatch()
        {
            // Arrange
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            ws1.Cell(1, 1).Value = "Data";
            // ws2 cell is empty

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert
            result.TextMismatches.Should().Be(1);
        }

        [Fact]
        public void CompareWorksheets_DifferentSizedWorksheets_ShouldCompareAllCells()
        {
            // Arrange
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            ws1.Cell(1, 1).Value = "A";
            ws1.Cell(2, 2).Value = "B";

            ws2.Cell(1, 1).Value = "A";
            ws2.Cell(3, 3).Value = "C";

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert
            result.TotalCellsCompared.Should().Be(9); // 3x3 grid
            result.TotalMismatches.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CompareWorksheets_MixedNumericAndText_ShouldCompareCorrectly()
        {
            // Arrange
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            ws1.Cell(1, 1).Value = 123;
            ws1.Cell(1, 2).Value = "Text";
            ws1.Cell(2, 1).Value = 456.789;

            ws2.Cell(1, 1).Value = 123;
            ws2.Cell(1, 2).Value = "Text";
            ws2.Cell(2, 1).Value = 456.789;

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert
            result.TotalMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareWorksheets_ZeroVsNegativeZero_ShouldMatch()
        {
            // Arrange
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            ws1.Cell(1, 1).Value = 0.0;
            ws2.Cell(1, 1).Value = -0.0;

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert
            result.NumericMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareWorksheets_CustomTolerance_ShouldRespectValue()
        {
            // Arrange
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            ws1.Cell(1, 1).Value = 1.0;
            ws2.Cell(1, 1).Value = 1.00001;

            var output1 = new StringWriter();
            Console.SetOut(output1);

            // Act - tolerance of 0.001 should match
            var resultLoose = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 0.001);

            // Act - tolerance of 1e-10 should not match
            var output2 = new StringWriter();
            Console.SetOut(output2);
            var resultStrict = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-10);

            // Assert
            resultLoose.NumericMismatches.Should().Be(0);
            resultStrict.NumericMismatches.Should().Be(1);
        }

        [Fact]
        public void CompareWorksheets_ShouldSetSheetsComparedToOne()
        {
            // Arrange
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert
            result.SheetsCompared.Should().Be(1);
        }

        [Fact]
        public void CompareWorksheets_LargeCellCount_ShouldUseLongForCount()
        {
            // Arrange
            var ws1 = CreateWorksheet();
            var ws2 = CreateWorksheet();

            // Create a 1000x1000 grid (1 million cells)
            for (int row = 1; row <= 1000; row++)
            {
                for (int col = 1; col <= 1000; col++)
                {
                    ws1.Cell(row, col).Value = row * col;
                    ws2.Cell(row, col).Value = row * col;
                }
            }

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CellComparer.CompareWorksheets(ws1, ws2, "TestSheet", 1e-15);

            // Assert
            result.TotalCellsCompared.Should().Be(1_000_000L);
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
