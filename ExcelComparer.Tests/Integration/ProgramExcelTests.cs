using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using Xunit;

namespace ExcelComparer.Tests.Integration
{
    public class ProgramExcelTests : IDisposable
    {
        private readonly List<string> _tempFiles = new();
        private readonly StringWriter _consoleOutput;
        private readonly TextWriter _originalOutput;

        public ProgramExcelTests()
        {
            _consoleOutput = new StringWriter();
            _originalOutput = Console.Out;
            Console.SetOut(_consoleOutput);
        }

        [Fact]
        public void Main_TwoExcelFiles_WithAllFlag_ShouldCompareAllSheets()
        {
            // Arrange
            var file1 = CreateExcelFile(wb =>
            {
                var sheet1 = wb.Worksheets.Add("Sheet1");
                sheet1.Cell(1, 1).Value = "A";
                var sheet2 = wb.Worksheets.Add("Sheet2");
                sheet2.Cell(1, 1).Value = "B";
            }, "file1.xlsx");

            var file2 = CreateExcelFile(wb =>
            {
                var sheet1 = wb.Worksheets.Add("Sheet1");
                sheet1.Cell(1, 1).Value = "A";
                var sheet2 = wb.Worksheets.Add("Sheet2");
                sheet2.Cell(1, 1).Value = "B";
            }, "file2.xlsx");

            // Act
            Program.Main(new[] { file1, file2, "--all" });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Comparing sheet: Sheet1");
            output.Should().Contain("Comparing sheet: Sheet2");
            output.Should().Contain("Sheets compared: 2");
        }

        [Fact]
        public void Main_TwoExcelFiles_WithSheetFlag_ShouldCompareSingleSheet()
        {
            // Arrange
            var file1 = CreateExcelFile(wb =>
            {
                var sheet1 = wb.Worksheets.Add("Sheet1");
                sheet1.Cell(1, 1).Value = "Test";
                var sheet2 = wb.Worksheets.Add("Data");
                sheet2.Cell(1, 1).Value = "Other";
            }, "file1.xlsx");

            var file2 = CreateExcelFile(wb =>
            {
                var sheet1 = wb.Worksheets.Add("Sheet1");
                sheet1.Cell(1, 1).Value = "Test";
                var sheet2 = wb.Worksheets.Add("Data");
                sheet2.Cell(1, 1).Value = "Different";
            }, "file2.xlsx");

            // Act
            Program.Main(new[] { file1, file2, "--sheet", "Data" });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Comparing sheet: Data");
            output.Should().Contain("Sheets compared: 1");
        }

        [Fact]
        public void Main_TwoExcelFiles_NoFlags_ShouldDefaultToSheet1()
        {
            // Arrange
            var file1 = CreateExcelFile(wb =>
            {
                var sheet = wb.Worksheets.Add("Sheet1");
                sheet.Cell(1, 1).Value = 100;
            }, "file1.xlsx");

            var file2 = CreateExcelFile(wb =>
            {
                var sheet = wb.Worksheets.Add("Sheet1");
                sheet.Cell(1, 1).Value = 100;
            }, "file2.xlsx");

            // Act
            Program.Main(new[] { file1, file2 });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Comparing sheet: Sheet1");
            output.Should().Contain("files are identical!");
        }

        [Fact]
        public void Main_ExcelFile_SheetNotFound_ShouldShowError()
        {
            // Arrange
            var file1 = CreateExcelFile(wb =>
            {
                var sheet = wb.Worksheets.Add("Sheet1");
                sheet.Cell(1, 1).Value = "Data";
            }, "file1.xlsx");

            var file2 = CreateExcelFile(wb =>
            {
                var sheet = wb.Worksheets.Add("Sheet1");
                sheet.Cell(1, 1).Value = "Data";
            }, "file2.xlsx");

            // Act
            Program.Main(new[] { file1, file2, "--sheet", "NonexistentSheet" });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Sheet 'NonexistentSheet' not found in one or both workbooks");
        }

        [Fact]
        public void Main_ExcelFile_NoCommonSheets_ShouldReportZeroSheetsCompared()
        {
            // Arrange
            var file1 = CreateExcelFile(wb =>
            {
                var sheet = wb.Worksheets.Add("DataA");
                sheet.Cell(1, 1).Value = "A";
            }, "file1.xlsx");

            var file2 = CreateExcelFile(wb =>
            {
                var sheet = wb.Worksheets.Add("DataB");
                sheet.Cell(1, 1).Value = "B";
            }, "file2.xlsx");

            // Act
            Program.Main(new[] { file1, file2, "--all" });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Sheets compared: 0");
        }

        [Fact]
        public void Main_ExcelWithCustomTolerance_ShouldUseTolerance()
        {
            // Arrange
            var file1 = CreateExcelFile(wb =>
            {
                var sheet = wb.Worksheets.Add("Sheet1");
                sheet.Cell(1, 1).Value = 1.0;
            }, "file1.xlsx");

            var file2 = CreateExcelFile(wb =>
            {
                var sheet = wb.Worksheets.Add("Sheet1");
                sheet.Cell(1, 1).Value = 1.01;
            }, "file2.xlsx");

            // Act
            Program.Main(new[] { file1, file2, "--tolerance", "0.05" });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Tolerance: 0.05");
            output.Should().Contain("files are identical!"); // Should match with tolerance of 0.05
        }

        [Fact]
        public void Main_EmptyExcelWorkbook_ShouldCompareSuccessfully()
        {
            // Arrange
            var file1 = CreateExcelFile(wb =>
            {
                wb.Worksheets.Add("Sheet1"); // Empty sheet
            }, "empty1.xlsx");

            var file2 = CreateExcelFile(wb =>
            {
                wb.Worksheets.Add("Sheet1"); // Empty sheet
            }, "empty2.xlsx");

            // Act
            Program.Main(new[] { file1, file2 });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Comparing sheet: Sheet1");
            output.Should().Contain("files are identical!");
        }

        [Fact]
        public void Main_ExcelFiles_AllAndSheetFlags_ShouldWarnAndUseAll()
        {
            // Arrange
            var file1 = CreateExcelFile(wb =>
            {
                var sheet1 = wb.Worksheets.Add("Sheet1");
                sheet1.Cell(1, 1).Value = 1;
                var sheet2 = wb.Worksheets.Add("Sheet2");
                sheet2.Cell(1, 1).Value = 2;
            }, "file1.xlsx");

            var file2 = CreateExcelFile(wb =>
            {
                var sheet1 = wb.Worksheets.Add("Sheet1");
                sheet1.Cell(1, 1).Value = 1;
                var sheet2 = wb.Worksheets.Add("Sheet2");
                sheet2.Cell(1, 1).Value = 2;
            }, "file2.xlsx");

            // Act
            Program.Main(new[] { file1, file2, "--all", "--sheet", "Sheet1" });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Warning: --sheet is ignored when --all is specified");
            output.Should().Contain("Comparing sheet: Sheet1");
            output.Should().Contain("Comparing sheet: Sheet2");
            output.Should().Contain("Sheets compared: 2");
        }

        [Fact]
        public void Main_ExcelFiles_DifferentData_ShouldReportMismatches()
        {
            // Arrange
            var file1 = CreateExcelFile(wb =>
            {
                var sheet = wb.Worksheets.Add("Sheet1");
                sheet.Cell(1, 1).Value = "Original";
                sheet.Cell(2, 1).Value = 100;
            }, "file1.xlsx");

            var file2 = CreateExcelFile(wb =>
            {
                var sheet = wb.Worksheets.Add("Sheet1");
                sheet.Cell(1, 1).Value = "Modified";
                sheet.Cell(2, 1).Value = 200;
            }, "file2.xlsx");

            // Act
            Program.Main(new[] { file1, file2 });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Text mismatch");
            output.Should().Contain("Numeric mismatch");
            output.Should().Contain("Total differences found: 2");
        }

        [Fact]
        public void Main_ExcelFiles_IdenticalMultipleSheets_ShouldReportAllIdentical()
        {
            // Arrange
            var file1 = CreateExcelFile(wb =>
            {
                for (int i = 1; i <= 3; i++)
                {
                    var sheet = wb.Worksheets.Add($"Sheet{i}");
                    sheet.Cell(1, 1).Value = $"Data{i}";
                }
            }, "file1.xlsx");

            var file2 = CreateExcelFile(wb =>
            {
                for (int i = 1; i <= 3; i++)
                {
                    var sheet = wb.Worksheets.Add($"Sheet{i}");
                    sheet.Cell(1, 1).Value = $"Data{i}";
                }
            }, "file2.xlsx");

            // Act
            Program.Main(new[] { file1, file2, "--all" });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Sheets compared: 3");
            output.Should().Contain("files are identical!");
        }

        [Fact]
        public void Main_ExcelWithMixedFileType_ShouldShowError()
        {
            // Arrange
            var excelFile = CreateExcelFile(wb =>
            {
                var sheet = wb.Worksheets.Add("Sheet1");
                sheet.Cell(1, 1).Value = "Data";
            }, "file.xlsx");

            var csvFile = Path.Combine(Path.GetTempPath(), "file_" + Guid.NewGuid() + ".csv");
            _tempFiles.Add(csvFile);
            File.WriteAllText(csvFile, "A,B\n1,2\n");

            // Act
            Program.Main(new[] { excelFile, csvFile });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Error: Both files must be the same type");
            output.Should().Contain("File 1: Excel");
            output.Should().Contain("File 2: Csv");
        }

        [Fact]
        public void Main_NonExistentExcelFile_ShouldShowError()
        {
            // Arrange
            var file1 = CreateExcelFile(wb =>
            {
                var sheet = wb.Worksheets.Add("Sheet1");
                sheet.Cell(1, 1).Value = "Data";
            }, "exists.xlsx");

            var file2 = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid() + ".xlsx");

            // Act
            Program.Main(new[] { file1, file2 });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Error: File not found");
            output.Should().Contain(file2);
        }

        private string CreateExcelFile(Action<XLWorkbook> configure, string fileName)
        {
            var uniqueFileName = Path.GetFileNameWithoutExtension(fileName) + "_" + Guid.NewGuid() + Path.GetExtension(fileName);
            var file = Path.Combine(Path.GetTempPath(), uniqueFileName);
            _tempFiles.Add(file);

            using (var workbook = new XLWorkbook())
            {
                configure(workbook);
                workbook.SaveAs(file);
            }

            return file;
        }

        public void Dispose()
        {
            Console.SetOut(_originalOutput);
            _consoleOutput?.Dispose();

            foreach (var file in _tempFiles)
            {
                if (File.Exists(file))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
    }
}
