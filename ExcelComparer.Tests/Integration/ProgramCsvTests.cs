using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace ExcelComparer.Tests.Integration
{
    public class ProgramCsvTests : IDisposable
    {
        private readonly List<string> _tempFiles = new();
        private readonly StringWriter _consoleOutput;
        private readonly TextWriter _originalOutput;

        public ProgramCsvTests()
        {
            _consoleOutput = new StringWriter();
            _originalOutput = Console.Out;
            Console.SetOut(_consoleOutput);
        }

        [Fact]
        public void Main_TwoCsvFiles_ShouldRouteToCompareCsvMode()
        {
            // Arrange
            var file1 = CreateCsvFile("A,B\n1,2\n", "data1.csv");
            var file2 = CreateCsvFile("A,B\n1,2\n", "data2.csv");

            // Act
            Program.Main(new[] { file1, file2 });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Comparing CSV files...");
            output.Should().Contain("File Type: Csv");
            output.Should().Contain("data1.csv vs data2.csv");
        }

        [Fact]
        public void Main_CsvFilesWithAllFlag_ShouldShowWarningAndCompare()
        {
            // Arrange
            var file1 = CreateCsvFile("A\n1\n", "file1.csv");
            var file2 = CreateCsvFile("A\n1\n", "file2.csv");

            // Act
            Program.Main(new[] { file1, file2, "--all" });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("--all flag is ignored for CSV files");
            output.Should().Contain("CSV files have no sheets");
            output.Should().Contain("Comparing CSV files...");
        }

        [Fact]
        public void Main_CsvFilesWithSheetFlag_ShouldShowWarningAndCompare()
        {
            // Arrange
            var file1 = CreateCsvFile("A\n1\n", "data1.csv");
            var file2 = CreateCsvFile("A\n1\n", "data2.csv");

            // Act
            Program.Main(new[] { file1, file2, "--sheet", "Sheet1" });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("--sheet flag is ignored for CSV files");
            output.Should().Contain("CSV files have no sheets");
            output.Should().Contain("Comparing CSV files...");
        }

        [Fact]
        public void Main_CsvFilesWithBothFlags_ShouldShowBothWarnings()
        {
            // Arrange
            var file1 = CreateCsvFile("A\n1\n", "test1.csv");
            var file2 = CreateCsvFile("A\n1\n", "test2.csv");

            // Act
            Program.Main(new[] { file1, file2, "--all", "--sheet", "MySheet" });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("--all flag is ignored for CSV files");
            output.Should().Contain("--sheet flag is ignored for CSV files");
            output.Should().Contain("Comparing CSV files...");
        }

        [Fact]
        public void Main_MixedFileTypes_ShouldShowErrorAndNotCompare()
        {
            // Arrange
            var csvFile = CreateCsvFile("A\n1\n", "data.csv");
            var xlsxPath = Path.Combine(Path.GetTempPath(), "dummy.xlsx");

            // Act
            Program.Main(new[] { csvFile, xlsxPath });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Error: Both files must be the same type");
            output.Should().Contain("File 1: Csv");
            output.Should().Contain("File 2: Excel");
            output.Should().NotContain("Comparing");
        }

        [Fact]
        public void Main_UnknownFileExtension_ShouldShowError()
        {
            // Arrange
            var file1 = CreateFileWithExtension("content", "file1.pdf");
            var file2 = CreateFileWithExtension("content", "file2.pdf");

            // Act
            Program.Main(new[] { file1, file2 });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Error: Unsupported file type");
            output.Should().Contain("Supported types: .xlsx, .xlsm, .xlsb, .xls, .csv, .txt");
            output.Should().NotContain("Comparing");
        }

        [Fact]
        public void Main_CsvWithCustomTolerance_ShouldUseTolerance()
        {
            // Arrange
            var file1 = CreateCsvFile("Value\n1.0\n", "file1.csv");
            var file2 = CreateCsvFile("Value\n1.01\n", "file2.csv");

            // Act
            Program.Main(new[] { file1, file2, "--tolerance", "0.05" });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Tolerance: 0.05");
            output.Should().Contain("Comparing CSV files...");
            // With tolerance of 0.05, difference of 0.01 should match
            output.Should().NotContain("mismatch");
        }

        [Fact]
        public void Main_TxtFilesTreatedAsCsv_ShouldCompare()
        {
            // Arrange
            var file1 = CreateFileWithExtension("A,B\n1,2\n", "data1.txt");
            var file2 = CreateFileWithExtension("A,B\n1,2\n", "data2.txt");

            // Act
            Program.Main(new[] { file1, file2 });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("File Type: Csv");
            output.Should().Contain("Comparing CSV files...");
        }

        [Fact]
        public void Main_MixedCsvAndTxt_ShouldTreatBothAsCsv()
        {
            // Arrange
            var csvFile = CreateCsvFile("A\n1\n", "data.csv");
            var txtFile = CreateFileWithExtension("A\n1\n", "data.txt");

            // Act
            Program.Main(new[] { csvFile, txtFile });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("File Type: Csv");
            output.Should().Contain("Comparing CSV files...");
            output.Should().NotContain("Error");
        }

        [Fact]
        public void Main_CsvFilesWithDifferences_ShouldReportSummary()
        {
            // Arrange
            var file1 = CreateCsvFile("A,B\n1,2\n3,4\n", "file1.csv");
            var file2 = CreateCsvFile("A,B\n1,99\n3,4\n", "file2.csv");

            // Act
            Program.Main(new[] { file1, file2 });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Comparing CSV files...");
            output.Should().Contain("Numeric mismatch");
            output.Should().Contain("Summary:");
            output.Should().Contain("Sheets compared: 1");
            output.Should().Contain("Numeric mismatches: 1");
        }

        [Fact]
        public void Main_IdenticalCsvFiles_ShouldReportNoMismatches()
        {
            // Arrange
            var file1 = CreateCsvFile("Name,Age\nAlice,25\nBob,30\n", "employees1.csv");
            var file2 = CreateCsvFile("Name,Age\nAlice,25\nBob,30\n", "employees2.csv");

            // Act
            Program.Main(new[] { file1, file2 });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Comparing CSV files...");
            output.Should().Contain("Files are identical!");
            output.Should().Contain("Total mismatches: 0");
        }

        [Fact]
        public void Main_NonExistentCsvFile_ShouldShowError()
        {
            // Arrange
            var file1 = CreateCsvFile("A\n1\n", "exists.csv");
            var file2 = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid() + ".csv");

            // Act
            Program.Main(new[] { file1, file2 });

            // Assert
            var output = _consoleOutput.ToString();
            output.Should().Contain("Error: File not found");
            output.Should().Contain(file2);
        }

        private string CreateCsvFile(string content, string fileName)
        {
            var file = Path.Combine(Path.GetTempPath(), fileName);
            _tempFiles.Add(file);
            File.WriteAllText(file, content, Encoding.UTF8);
            return file;
        }

        private string CreateFileWithExtension(string content, string fileName)
        {
            var file = Path.Combine(Path.GetTempPath(), fileName);
            _tempFiles.Add(file);
            File.WriteAllText(file, content, Encoding.UTF8);
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
