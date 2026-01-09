using ExcelComparer.Utilities;
using FluentAssertions;
using Xunit;

namespace ExcelComparer.Tests.Unit
{
    public class FileTypeDetectorTests
    {
        [Theory]
        [InlineData("file.xlsx", FileTypeDetector.FileType.Excel)]
        [InlineData("file.xlsm", FileTypeDetector.FileType.Excel)]
        [InlineData("file.xlsb", FileTypeDetector.FileType.Excel)]
        [InlineData("file.xls", FileTypeDetector.FileType.Excel)]
        [InlineData("FILE.XLSX", FileTypeDetector.FileType.Excel)] // Case insensitive
        public void DetectFileType_ExcelFiles_ShouldReturnExcel(string fileName, FileTypeDetector.FileType expected)
        {
            // Act
            var result = FileTypeDetector.DetectFileType(fileName);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("file.csv", FileTypeDetector.FileType.Csv)]
        [InlineData("file.txt", FileTypeDetector.FileType.Csv)]
        [InlineData("FILE.CSV", FileTypeDetector.FileType.Csv)] // Case insensitive
        public void DetectFileType_CsvFiles_ShouldReturnCsv(string fileName, FileTypeDetector.FileType expected)
        {
            // Act
            var result = FileTypeDetector.DetectFileType(fileName);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("file.doc")]
        [InlineData("file.pdf")]
        [InlineData("file")]
        [InlineData("")]
        public void DetectFileType_UnknownFiles_ShouldReturnUnknown(string? fileName)
        {
            // Act
            var result = FileTypeDetector.DetectFileType(fileName);

            // Assert
            result.Should().Be(FileTypeDetector.FileType.Unknown);
        }

        [Theory]
        [InlineData("file1.xlsx", "file2.xlsx", true)]
        [InlineData("file1.csv", "file2.csv", true)]
        [InlineData("file1.xlsm", "file2.xlsx", true)] // Both Excel
        [InlineData("file1.xlsx", "file2.csv", false)]
        [InlineData("file1.csv", "file2.xlsx", false)]
        public void AreSameType_VariousFiles_ShouldReturnCorrectResult(string file1, string file2, bool expected)
        {
            // Act
            var result = FileTypeDetector.AreSameType(file1, file2);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void AreExcelFiles_BothExcel_ShouldReturnTrue()
        {
            // Act
            var result = FileTypeDetector.AreExcelFiles("file1.xlsx", "file2.xlsm");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void AreExcelFiles_OneCsv_ShouldReturnFalse()
        {
            // Act
            var result = FileTypeDetector.AreExcelFiles("file1.xlsx", "file2.csv");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void AreCsvFiles_BothCsv_ShouldReturnTrue()
        {
            // Act
            var result = FileTypeDetector.AreCsvFiles("file1.csv", "file2.txt");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void AreCsvFiles_OneExcel_ShouldReturnFalse()
        {
            // Act
            var result = FileTypeDetector.AreCsvFiles("file1.csv", "file2.xlsx");

            // Assert
            result.Should().BeFalse();
        }

        // Edge Case Tests (Priority 3)

        [Theory]
        [InlineData("file<>.csv")]
        [InlineData("file?.xlsx")]
        [InlineData("file|data.txt")]
        [InlineData("C:\\invalid\\path\\file*.csv")]
        public void DetectFileType_InvalidPathCharacters_ShouldHandleGracefully(string invalidPath)
        {
            // Act - should not throw exception
            Action act = () => FileTypeDetector.DetectFileType(invalidPath);

            // Assert - should handle gracefully (return any valid FileType)
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData("report.backup.csv", FileTypeDetector.FileType.Csv)]
        [InlineData("data.old.xlsx", FileTypeDetector.FileType.Excel)]
        [InlineData("file.2024.01.15.txt", FileTypeDetector.FileType.Csv)]
        [InlineData("archive.v1.xlsm", FileTypeDetector.FileType.Excel)]
        public void DetectFileType_MultipleDotsInFilename_ShouldUseLastExtension(string fileName, FileTypeDetector.FileType expected)
        {
            // Act
            var result = FileTypeDetector.DetectFileType(fileName);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void DetectFileType_NullString_ShouldReturnUnknown()
        {
            // Act
            var result = FileTypeDetector.DetectFileType(null);

            // Assert
            result.Should().Be(FileTypeDetector.FileType.Unknown);
        }

        [Fact]
        public void DetectFileType_WhitespaceOnly_ShouldReturnUnknown()
        {
            // Act
            var result = FileTypeDetector.DetectFileType("   ");

            // Assert
            result.Should().Be(FileTypeDetector.FileType.Unknown);
        }

        [Fact]
        public void AreSameType_BothNull_ShouldReturnFalse()
        {
            // Act
            var result = FileTypeDetector.AreSameType(null, null);

            // Assert
            result.Should().BeFalse(); // Both are Unknown, but Unknown never matches
        }

        [Theory]
        [InlineData(null, "file.csv")]
        [InlineData("file.xlsx", null)]
        public void AreSameType_OneNull_ShouldReturnFalse(string? file1, string? file2)
        {
            // Act
            var result = FileTypeDetector.AreSameType(file1, file2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void AreExcelFiles_BothNull_ShouldReturnFalse()
        {
            // Act
            var result = FileTypeDetector.AreExcelFiles(null, null);

            // Assert
            result.Should().BeFalse(); // Neither is Excel
        }

        [Fact]
        public void AreCsvFiles_BothNull_ShouldReturnFalse()
        {
            // Act
            var result = FileTypeDetector.AreCsvFiles(null, null);

            // Assert
            result.Should().BeFalse(); // Neither is CSV
        }
    }
}
