using ExcelComparer.Modes;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace ExcelComparer.Tests.Unit
{
    public class CompareCsvModeTests : IDisposable
    {
        private readonly List<string> _tempFiles = new();
        private readonly TextWriter _originalOutput;

        public CompareCsvModeTests()
        {
            _originalOutput = Console.Out;
        }

        [Fact]
        public void Run_ValidIdenticalFiles_ShouldReturnComparisonResultWithNoMismatches()
        {
            // Arrange
            var file1 = CreateCsvFile("A,B\n1,2\n", "test1.csv");
            var file2 = CreateCsvFile("A,B\n1,2\n", "test2.csv");

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CompareCsv.Run(file1, file2, 1e-15);

            // Assert
            result.Should().NotBeNull();
            result.SheetsCompared.Should().Be(1);
            result.TotalCellsCompared.Should().Be(4); // 2 rows x 2 columns
            result.TotalMismatches.Should().Be(0);
        }

        [Fact]
        public void Run_ValidDifferentFiles_ShouldReturnComparisonResultWithMismatches()
        {
            // Arrange
            var file1 = CreateCsvFile("A\n1\n", "file1.csv");
            var file2 = CreateCsvFile("A\n2\n", "file2.csv");

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CompareCsv.Run(file1, file2, 1e-15);

            // Assert
            result.Should().NotBeNull();
            result.SheetsCompared.Should().Be(1);
            result.NumericMismatches.Should().Be(1);
        }

        [Fact]
        public void Run_ShouldDisplayComparingMessage()
        {
            // Arrange
            var file1 = CreateCsvFile("A\n1\n", "data1.csv");
            var file2 = CreateCsvFile("A\n1\n", "data2.csv");

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            CompareCsv.Run(file1, file2, 1e-15);

            // Assert
            var consoleOutput = output.ToString();
            consoleOutput.Should().Contain("Comparing CSV files...");
        }

        [Fact]
        public void Run_ShouldFormatIdentifierWithFileNames()
        {
            // Arrange
            var file1 = CreateCsvFile("A\n1\n", "report_v1.csv");
            var file2 = CreateCsvFile("A\n2\n", "report_v2.csv");

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            CompareCsv.Run(file1, file2, 1e-15);

            // Assert
            var consoleOutput = output.ToString();
            consoleOutput.Should().Contain("report_v1.csv vs report_v2.csv");
        }

        [Fact]
        public void Run_WithFullPaths_ShouldUseOnlyFileNamesInIdentifier()
        {
            // Arrange
            var dir1 = Path.Combine(Path.GetTempPath(), "dir1");
            var dir2 = Path.Combine(Path.GetTempPath(), "dir2");
            Directory.CreateDirectory(dir1);
            Directory.CreateDirectory(dir2);

            var file1 = Path.Combine(dir1, "data.csv");
            var file2 = Path.Combine(dir2, "data.csv");

            File.WriteAllText(file1, "A\n1\n", Encoding.UTF8);
            File.WriteAllText(file2, "A\n2\n", Encoding.UTF8);

            _tempFiles.Add(file1);
            _tempFiles.Add(file2);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            CompareCsv.Run(file1, file2, 1e-15);

            // Assert
            var consoleOutput = output.ToString();
            consoleOutput.Should().Contain("data.csv vs data.csv");
            consoleOutput.Should().NotContain("dir1");
            consoleOutput.Should().NotContain("dir2");

            // Cleanup directories
            try { Directory.Delete(dir1, true); } catch { }
            try { Directory.Delete(dir2, true); } catch { }
        }

        [Fact]
        public void Run_WithCustomTolerance_ShouldPassToleranceToCsvComparer()
        {
            // Arrange
            var file1 = CreateCsvFile("Value\n1.0\n", "file1.csv");
            var file2 = CreateCsvFile("Value\n1.0001\n", "file2.csv");

            var output1 = new StringWriter();
            Console.SetOut(output1);

            // Act - with loose tolerance, should match
            var resultLoose = CompareCsv.Run(file1, file2, 0.001);

            var output2 = new StringWriter();
            Console.SetOut(output2);

            // Act - with strict tolerance, should not match
            var resultStrict = CompareCsv.Run(file1, file2, 1e-15);

            // Assert
            resultLoose.NumericMismatches.Should().Be(0);
            resultStrict.NumericMismatches.Should().Be(1);
        }

        [Fact]
        public void Run_WithNonExistentFile_ShouldHandleGracefully()
        {
            // Arrange
            var file1 = CreateCsvFile("A\n1\n", "exists.csv");
            var file2 = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid() + ".csv");

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CompareCsv.Run(file1, file2, 1e-15);

            // Assert - should return result with SheetsCompared = 0 (error state)
            result.Should().NotBeNull();
            result.SheetsCompared.Should().Be(0);
            output.ToString().Should().Contain("Error");
        }

        [Fact]
        public void Run_WithEmptyFiles_ShouldCompareSuccessfully()
        {
            // Arrange
            var file1 = CreateCsvFile("", "empty1.csv");
            var file2 = CreateCsvFile("", "empty2.csv");

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CompareCsv.Run(file1, file2, 1e-15);

            // Assert
            result.Should().NotBeNull();
            result.SheetsCompared.Should().Be(1);
            result.TotalCellsCompared.Should().Be(0);
            result.TotalMismatches.Should().Be(0);
        }

        private string CreateCsvFile(string content, string fileName)
        {
            // Make filename unique to avoid conflicts when tests run in parallel
            var uniqueFileName = Path.GetFileNameWithoutExtension(fileName) + "_" + Guid.NewGuid() + Path.GetExtension(fileName);
            var file = Path.Combine(Path.GetTempPath(), uniqueFileName);
            _tempFiles.Add(file);
            File.WriteAllText(file, content, Encoding.UTF8);
            return file;
        }

        public void Dispose()
        {
            Console.SetOut(_originalOutput);

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
