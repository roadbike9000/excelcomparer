using ExcelComparer.Utilities;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace ExcelComparer.Tests.Unit
{
    public class CsvComparerDosProtectionTests : IDisposable
    {
        private readonly List<string> _tempFiles = new();

        [Fact]
        public void CompareCsvFiles_FileSizeExceeds100MB_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var largeCsvFile = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(largeCsvFile);

            // Create a file > 100MB using sparse file technique
            using (var fs = new FileStream(largeCsvFile, FileMode.Create))
            {
                fs.SetLength(101L * 1024 * 1024); // 101 MB
            }

            var smallFile = CreateSmallCsvFile();

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            Action act = () => CsvComparer.CompareCsvFiles(largeCsvFile, smallFile, "test", 1e-15);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*exceeds maximum allowed size*");
        }

        [Fact]
        public void CompareCsvFiles_FileSizeJustUnder100MB_ShouldSucceed()
        {
            // Arrange
            var almostLargeFile = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(almostLargeFile);

            // Create a file just under 100MB (99MB)
            using (var fs = new FileStream(almostLargeFile, FileMode.Create))
            {
                fs.SetLength(99L * 1024 * 1024); // 99 MB
                // Write minimal valid CSV header
                var header = Encoding.UTF8.GetBytes("A\n");
                fs.Write(header, 0, header.Length);
            }

            var smallFile = CreateSmallCsvFile();

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            Action act = () => CsvComparer.CompareCsvFiles(almostLargeFile, smallFile, "test", 1e-15);

            // Assert - should not throw, though might have mismatches
            act.Should().NotThrow();
        }

        [Fact]
        [Trait("Category", "Slow")]
        public void CompareCsvFiles_RowCountExceeds1Million_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var hugeRowFile = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(hugeRowFile);

            // Create CSV with 1,000,001 rows
            using (var writer = new StreamWriter(hugeRowFile, false, Encoding.UTF8))
            {
                writer.WriteLine("Column1");
                for (int i = 0; i <= 1_000_000; i++)
                {
                    writer.WriteLine($"{i}");
                }
            }

            var smallFile = CreateSmallCsvFile();

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            Action act = () => CsvComparer.CompareCsvFiles(hugeRowFile, smallFile, "test", 1e-15);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*exceeds the maximum allowed*");
        }

        [Fact]
        [Trait("Category", "Slow")]
        public void CompareCsvFiles_Exactly1MillionRows_ShouldSucceed()
        {
            // Arrange
            var maxRowFile = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(maxRowFile);

            // Create CSV with exactly 1,000,000 rows (boundary test)
            using (var writer = new StreamWriter(maxRowFile, false, Encoding.UTF8))
            {
                writer.WriteLine("Column1");
                for (int i = 0; i < 999_999; i++) // Header + 999,999 = 1,000,000 total
                {
                    writer.WriteLine($"{i}");
                }
            }

            var smallFile = CreateSmallCsvFile();

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            Action act = () => CsvComparer.CompareCsvFiles(maxRowFile, smallFile, "test", 1e-15);

            // Assert - should not throw
            act.Should().NotThrow();
        }

        [Fact]
        public void CompareCsvFiles_SecondFileSizeExceeds100MB_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var smallFile = CreateSmallCsvFile();

            var largeCsvFile = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(largeCsvFile);

            using (var fs = new FileStream(largeCsvFile, FileMode.Create))
            {
                fs.SetLength(101L * 1024 * 1024); // 101 MB
            }

            var output = new StringWriter();
            Console.SetOut(output);

            // Act - test second file being too large
            Action act = () => CsvComparer.CompareCsvFiles(smallFile, largeCsvFile, "test", 1e-15);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*exceeds maximum allowed size*");
        }

        private string CreateSmallCsvFile()
        {
            var file = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(file);
            File.WriteAllText(file, "A\n1\n", Encoding.UTF8);
            return file;
        }

        public void Dispose()
        {
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
