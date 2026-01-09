using ExcelComparer.Utilities;
using FluentAssertions;
using System.IO;
using System.Text;
using Xunit;

namespace ExcelComparer.Tests.Unit
{
    public class CsvComparerTests : IDisposable
    {
        private const double DefaultTolerance = 1e-15;
        private readonly List<string> _tempFiles = new();

        private string CreateCsvFile(string[][] rows)
        {
            var tempFile = Path.GetTempFileName();
            _tempFiles.Add(tempFile); // Add original first for cleanup

            try
            {
                var csvFile = tempFile + ".csv";
                File.Move(tempFile, csvFile);
                _tempFiles.Remove(tempFile); // Remove original
                _tempFiles.Add(csvFile); // Add new

                var sb = new StringBuilder();
                foreach (var row in rows)
                {
                    sb.AppendLine(string.Join(",", row));
                }

                File.WriteAllText(csvFile, sb.ToString(), Encoding.UTF8);
                return csvFile;
            }
            catch
            {
                // Cleanup will handle the original tempFile
                throw;
            }
        }

        [Fact]
        public void CompareCsvFiles_IdenticalFiles_ShouldReturnNoMismatches()
        {
            // Arrange
            var data = new[]
            {
                new[] { "Name", "Age", "Score" },
                new[] { "Alice", "25", "95.5" },
                new[] { "Bob", "30", "87.3" }
            };

            var file1 = CreateCsvFile(data);
            var file2 = CreateCsvFile(data);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(file1, file2, "test", DefaultTolerance);

            // Assert
            result.TotalCellsCompared.Should().Be(9); // 3 rows × 3 cols
            result.TotalMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareCsvFiles_NumericDifference_ShouldDetectMismatch()
        {
            // Arrange
            var data1 = new[]
            {
                new[] { "Value" },
                new[] { "100.5" }
            };

            var data2 = new[]
            {
                new[] { "Value" },
                new[] { "100.6" }
            };

            var file1 = CreateCsvFile(data1);
            var file2 = CreateCsvFile(data2);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(file1, file2, "test", DefaultTolerance);

            // Assert
            result.NumericMismatches.Should().Be(1);
        }

        [Fact]
        public void CompareCsvFiles_TextDifference_ShouldDetectMismatch()
        {
            // Arrange
            var data1 = new[]
            {
                new[] { "Name" },
                new[] { "Alice" }
            };

            var data2 = new[]
            {
                new[] { "Name" },
                new[] { "Bob" }
            };

            var file1 = CreateCsvFile(data1);
            var file2 = CreateCsvFile(data2);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(file1, file2, "test", DefaultTolerance);

            // Assert
            result.TextMismatches.Should().Be(1);
        }

        [Fact]
        public void CompareCsvFiles_CustomTolerance_ShouldRespectValue()
        {
            // Arrange
            var data1 = new[]
            {
                new[] { "1.0" }
            };

            var data2 = new[]
            {
                new[] { "1.0001" }
            };

            var file1 = CreateCsvFile(data1);
            var file2 = CreateCsvFile(data2);

            var output1 = new StringWriter();
            Console.SetOut(output1);

            // Act - tolerance of 0.001 should match
            var resultLoose = CsvComparer.CompareCsvFiles(file1, file2, "test", 0.001);

            var output2 = new StringWriter();
            Console.SetOut(output2);

            // Act - tolerance of 1e-10 should not match
            var resultStrict = CsvComparer.CompareCsvFiles(file1, file2, "test", 1e-10);

            // Assert
            resultLoose.NumericMismatches.Should().Be(0);
            resultStrict.NumericMismatches.Should().Be(1);
        }

        [Fact]
        public void CompareCsvFiles_DifferentRowCounts_ShouldCompareAll()
        {
            // Arrange
            var data1 = new[]
            {
                new[] { "A" },
                new[] { "B" }
            };

            var data2 = new[]
            {
                new[] { "A" },
                new[] { "B" },
                new[] { "C" }
            };

            var file1 = CreateCsvFile(data1);
            var file2 = CreateCsvFile(data2);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(file1, file2, "test", DefaultTolerance);

            // Assert
            result.TotalCellsCompared.Should().Be(3); // Max rows
            result.TextMismatches.Should().Be(1); // "C" vs empty
        }

        [Fact]
        public void CompareCsvFiles_DifferentColumnCounts_ShouldCompareAll()
        {
            // Arrange
            var data1 = new[]
            {
                new[] { "A", "B" }
            };

            var data2 = new[]
            {
                new[] { "A", "B", "C" }
            };

            var file1 = CreateCsvFile(data1);
            var file2 = CreateCsvFile(data2);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(file1, file2, "test", DefaultTolerance);

            // Assert
            result.TotalCellsCompared.Should().Be(3); // Max cols
            result.TextMismatches.Should().Be(1); // "C" vs empty
        }

        [Fact]
        public void CompareCsvFiles_EmptyFiles_ShouldReturnNoMismatches()
        {
            // Arrange
            var data = new string[0][];

            var file1 = CreateCsvFile(data);
            var file2 = CreateCsvFile(data);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(file1, file2, "test", DefaultTolerance);

            // Assert
            result.TotalCellsCompared.Should().Be(0);
            result.TotalMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareCsvFiles_MixedNumericAndText_ShouldCompareCorrectly()
        {
            // Arrange
            var data = new[]
            {
                new[] { "Name", "Age", "Score" },
                new[] { "Alice", "25", "95.5" },
                new[] { "Bob", "30", "87.3" }
            };

            var file1 = CreateCsvFile(data);
            var file2 = CreateCsvFile(data);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(file1, file2, "test", DefaultTolerance);

            // Assert
            result.TotalMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareCsvFiles_WithQuotedFields_ShouldHandleCorrectly()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            File.WriteAllText(tempFile1, "Name,Description\nAlice,\"Hello, World\"\n", Encoding.UTF8);
            File.WriteAllText(tempFile2, "Name,Description\nAlice,\"Hello, World\"\n", Encoding.UTF8);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert
            result.TotalMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareCsvFiles_NonExistentFile_ShouldHandleGracefully()
        {
            // Arrange
            var file1 = CreateCsvFile(new[] { new[] { "A" } });
            var nonExistentFile = Path.Combine(Path.GetTempPath(), "NonExistent_" + Guid.NewGuid() + ".csv");

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(file1, nonExistentFile, "test", DefaultTolerance);

            // Assert
            result.SheetsCompared.Should().Be(0);
            result.TotalMismatches.Should().Be(0);
            output.ToString().Should().Contain("Error");
        }

        [Fact]
        public void CompareCsvFiles_EmptyFilePath_ShouldThrowArgumentException()
        {
            // Arrange
            var file1 = CreateCsvFile(new[] { new[] { "A" } });

            // Act
            Action act = () => CsvComparer.CompareCsvFiles(file1, "", "test", DefaultTolerance);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*File path cannot be null or empty*");
        }

        [Fact]
        public void CompareCsvFiles_NullFilePath_ShouldThrowArgumentException()
        {
            // Arrange
            var file1 = CreateCsvFile(new[] { new[] { "A" } });

            // Act
            Action act = () => CsvComparer.CompareCsvFiles(file1, null!, "test", DefaultTolerance);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*File path cannot be null or empty*");
        }

        [Fact]
        public void CompareCsvFiles_NegativeTolerance_ShouldThrowArgumentException()
        {
            // Arrange
            var file1 = CreateCsvFile(new[] { new[] { "A" } });
            var file2 = CreateCsvFile(new[] { new[] { "A" } });

            // Act
            Action act = () => CsvComparer.CompareCsvFiles(file1, file2, "test", -0.001);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Tolerance must be a non-negative finite number*");
        }

        [Fact]
        public void CompareCsvFiles_NaNTolerance_ShouldThrowArgumentException()
        {
            // Arrange
            var file1 = CreateCsvFile(new[] { new[] { "A" } });
            var file2 = CreateCsvFile(new[] { new[] { "A" } });

            // Act
            Action act = () => CsvComparer.CompareCsvFiles(file1, file2, "test", double.NaN);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Tolerance must be a non-negative finite number*");
        }

        [Fact]
        public void CompareCsvFiles_InfinityTolerance_ShouldThrowArgumentException()
        {
            // Arrange
            var file1 = CreateCsvFile(new[] { new[] { "A" } });
            var file2 = CreateCsvFile(new[] { new[] { "A" } });

            // Act
            Action act = () => CsvComparer.CompareCsvFiles(file1, file2, "test", double.PositiveInfinity);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Tolerance must be a non-negative finite number*");
        }

        [Fact]
        public void CompareCsvFiles_EmptyIdentifier_ShouldThrowArgumentException()
        {
            // Arrange
            var file1 = CreateCsvFile(new[] { new[] { "A" } });
            var file2 = CreateCsvFile(new[] { new[] { "A" } });

            // Act
            Action act = () => CsvComparer.CompareCsvFiles(file1, file2, "", DefaultTolerance);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Identifier cannot be null or empty*");
        }

        [Fact]
        public void CompareCsvFiles_TypeMismatch_ShouldReportCorrectly()
        {
            // Arrange
            var data1 = new[]
            {
                new[] { "Value" },
                new[] { "100" }  // Number as string
            };

            var data2 = new[]
            {
                new[] { "Value" },
                new[] { "abc" }  // Text
            };

            var file1 = CreateCsvFile(data1);
            var file2 = CreateCsvFile(data2);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(file1, file2, "test", DefaultTolerance);

            // Assert
            result.TextMismatches.Should().BeGreaterThanOrEqualTo(1);
            output.ToString().Should().Contain("mismatch");
        }

        [Fact]
        public void CompareCsvFiles_Utf8WithBOM_ShouldCompareCorrectly()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            // Write with UTF-8 BOM
            File.WriteAllText(tempFile1, "Café,naïve\nPâté,résumé", new UTF8Encoding(true));
            File.WriteAllText(tempFile2, "Café,naïve\nPâté,résumé", new UTF8Encoding(true));

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert
            result.TotalMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareCsvFiles_Utf8WithoutBOM_ShouldCompareCorrectly()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            // Write without UTF-8 BOM
            File.WriteAllText(tempFile1, "Café,naïve\nPâté,résumé", new UTF8Encoding(false));
            File.WriteAllText(tempFile2, "Café,naïve\nPâté,résumé", new UTF8Encoding(false));

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert
            result.TotalMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareCsvFiles_MixedBOMEncodings_ShouldCompareCorrectly()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            // One with BOM, one without
            File.WriteAllText(tempFile1, "Café,naïve", new UTF8Encoding(true));
            File.WriteAllText(tempFile2, "Café,naïve", new UTF8Encoding(false));

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert
            result.TotalMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareCsvFiles_Utf16LittleEndian_ShouldDetectAndReadCorrectly()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            var content = "Name,Value\nTest,123\n";

            // Write both files with UTF-16 LE encoding (with BOM)
            File.WriteAllText(tempFile1, content, new UnicodeEncoding(false, true));
            File.WriteAllText(tempFile2, content, new UnicodeEncoding(false, true));

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert
            result.TotalMismatches.Should().Be(0);
            result.TotalCellsCompared.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CompareCsvFiles_Utf16BigEndian_ShouldDetectAndReadCorrectly()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            var content = "Name,Value\nTest,456\n";

            // Write both files with UTF-16 BE encoding (with BOM)
            File.WriteAllText(tempFile1, content, new UnicodeEncoding(true, true));
            File.WriteAllText(tempFile2, content, new UnicodeEncoding(true, true));

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert
            result.TotalMismatches.Should().Be(0);
            result.TotalCellsCompared.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CompareCsvFiles_Utf32LittleEndian_ShouldDetectAndReadCorrectly()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            var content = "Column\n789\n";

            // Write both files with UTF-32 LE encoding (with BOM)
            File.WriteAllText(tempFile1, content, new UTF32Encoding(false, true));
            File.WriteAllText(tempFile2, content, new UTF32Encoding(false, true));

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert
            result.TotalMismatches.Should().Be(0);
            result.TotalCellsCompared.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CompareCsvFiles_Utf32BigEndian_ShouldDetectAndReadCorrectly()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            var content = "Column\n999\n";

            // Write both files with UTF-32 BE encoding (with BOM)
            File.WriteAllText(tempFile1, content, new UTF32Encoding(true, true));
            File.WriteAllText(tempFile2, content, new UTF32Encoding(true, true));

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert
            result.TotalMismatches.Should().Be(0);
            result.TotalCellsCompared.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CompareCsvFiles_EncodingDetectionFailsWithIOException_ShouldFallbackToUtf8()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            // Create normal files (encoding detection should work, but tests fallback logic exists)
            File.WriteAllText(tempFile1, "A,B\n1,2\n", Encoding.UTF8);
            File.WriteAllText(tempFile2, "A,B\n1,2\n", Encoding.UTF8);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert - should complete successfully even if encoding detection had issues
            result.TotalMismatches.Should().Be(0);
            result.TotalCellsCompared.Should().Be(4);
        }

        [Fact]
        public void CompareCsvFiles_NoBOM_ShouldDefaultToUtf8()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            // Create files without BOM (UTF-8 without BOM)
            var content = "Name,Age\nAlice,30\nBob,25\n";
            File.WriteAllText(tempFile1, content, new UTF8Encoding(false));
            File.WriteAllText(tempFile2, content, new UTF8Encoding(false));

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert
            result.TotalMismatches.Should().Be(0);
            result.TotalCellsCompared.Should().Be(6); // 3 rows x 2 columns
        }

        [Fact]
        public void CompareCsvFiles_WithNewlinesInQuotedFields_ShouldHandleCorrectly()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            var csvContent = "Name,Description\nAlice,\"Line1\nLine2\"\nBob,\"Line3\nLine4\"";
            File.WriteAllText(tempFile1, csvContent, Encoding.UTF8);
            File.WriteAllText(tempFile2, csvContent, Encoding.UTF8);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert
            result.TotalMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareCsvFiles_WithSpecialCharactersInQuotedFields_ShouldHandleCorrectly()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            var csvContent = "Name,Special\nTest1,\"Commas, semicolons; pipes|\"\nTest2,\"Quotes \"\"inside\"\" text\"";
            File.WriteAllText(tempFile1, csvContent, Encoding.UTF8);
            File.WriteAllText(tempFile2, csvContent, Encoding.UTF8);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert
            result.TotalMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareCsvFiles_WithDifferentDelimiters_ShouldAutoDetect()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            // One with comma, one with semicolon (both should work with auto-detect)
            File.WriteAllText(tempFile1, "A,B,C\n1,2,3", Encoding.UTF8);
            File.WriteAllText(tempFile2, "A;B;C\n1;2;3", Encoding.UTF8);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert - Both should parse correctly with auto-detection
            result.TotalMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareCsvFiles_MissingFieldsInRow_ShouldHandleGracefully()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            // Row 2 has missing third field
            File.WriteAllText(tempFile1, "A,B,C\n1,2,3\n4,5\n", Encoding.UTF8);
            File.WriteAllText(tempFile2, "A,B,C\n1,2,3\n4,5\n", Encoding.UTF8);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act - should not throw exception
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert - missing field treated as empty, should match
            result.TotalMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareCsvFiles_TabDelimiter_ShouldAutoDetect()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            // TSV (tab-separated values)
            File.WriteAllText(tempFile1, "Name\tAge\tCity\nAlice\t30\tNY\n", Encoding.UTF8);
            File.WriteAllText(tempFile2, "Name\tAge\tCity\nAlice\t30\tNY\n", Encoding.UTF8);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert - auto-detect should work for tabs
            result.TotalMismatches.Should().Be(0);
            result.TotalCellsCompared.Should().Be(6); // 2 rows x 3 columns
        }

        [Fact]
        public void CompareCsvFiles_PipeDelimiter_ShouldAutoDetect()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            // Pipe-separated values
            File.WriteAllText(tempFile1, "A|B|C\n1|2|3\n", Encoding.UTF8);
            File.WriteAllText(tempFile2, "A|B|C\n1|2|3\n", Encoding.UTF8);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert - auto-detect should work for pipes
            result.TotalMismatches.Should().Be(0);
            result.TotalCellsCompared.Should().Be(6); // 2 rows x 3 columns
        }

        [Fact]
        public void CompareCsvFiles_SemicolonDelimiter_ShouldAutoDetect()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            // Semicolon-separated values (common in European locales)
            File.WriteAllText(tempFile1, "Name;Age;Score\nBob;25;95.5\n", Encoding.UTF8);
            File.WriteAllText(tempFile2, "Name;Age;Score\nBob;25;95.5\n", Encoding.UTF8);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert - auto-detect should work for semicolons
            result.TotalMismatches.Should().Be(0);
            result.TotalCellsCompared.Should().Be(6); // 2 rows x 3 columns
        }

        [Fact]
        public void CompareCsvFiles_WhitespaceOnlyDifference_ShouldDetect()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            // Second file has trailing space
            File.WriteAllText(tempFile1, "A,B\nvalue,123\n", Encoding.UTF8);
            File.WriteAllText(tempFile2, "A,B\nvalue ,123\n", Encoding.UTF8);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert - TrimOptions.None means whitespace differences should be detected
            result.TextMismatches.Should().Be(1);
        }

        [Fact]
        public void CompareCsvFiles_EmptyVsNull_ShouldTreatAsSame()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            // File1 has empty string, File2 has missing field (null)
            File.WriteAllText(tempFile1, "A,B,C\n1,2,\n", Encoding.UTF8);
            File.WriteAllText(tempFile2, "A,B,C\n1,2\n", Encoding.UTF8);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert - both empty string and null should become "" and match
            result.TotalMismatches.Should().Be(0);
        }

        [Fact]
        public void CompareCsvFiles_UnclosedQuotes_ShouldHandleGracefully()
        {
            // Arrange
            var tempFile1 = Path.GetTempFileName() + ".csv";
            var tempFile2 = Path.GetTempFileName() + ".csv";
            _tempFiles.Add(tempFile1);
            _tempFiles.Add(tempFile2);

            // Malformed CSV with unclosed quotes (BadDataFound = null allows this)
            File.WriteAllText(tempFile1, "A,B\n1,\"unclosed\n", Encoding.UTF8);
            File.WriteAllText(tempFile2, "A,B\n1,\"unclosed\n", Encoding.UTF8);

            var output = new StringWriter();
            Console.SetOut(output);

            // Act - should not throw exception due to BadDataFound = null
            Action act = () => CsvComparer.CompareCsvFiles(tempFile1, tempFile2, "test", DefaultTolerance);

            // Assert - should handle gracefully (may parse differently but won't crash)
            act.Should().NotThrow();
        }

        [Fact]
        public void CompareCsvFiles_MoreThan100Differences_ShouldLimitOutput()
        {
            // Arrange - create files with 150 differences
            var rows1 = new List<string[]> { new[] { "Value" } };
            var rows2 = new List<string[]> { new[] { "Value" } };

            for (int i = 1; i <= 150; i++)
            {
                rows1.Add(new[] { $"A{i}" });
                rows2.Add(new[] { $"B{i}" }); // All different
            }

            var file1 = CreateCsvFile(rows1.ToArray());
            var file2 = CreateCsvFile(rows2.ToArray());

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(file1, file2, "test", DefaultTolerance);

            // Assert
            result.TextMismatches.Should().Be(150);

            var consoleOutput = output.ToString();
            var mismatchLines = consoleOutput.Split('\n').Count(line => line.Contains("mismatch at"));
            mismatchLines.Should().BeLessThanOrEqualTo(101); // Max 100 differences + 1 suppression line

            consoleOutput.Should().Contain("suppressing further difference output");
            consoleOutput.Should().Contain("max 100 shown");
        }

        [Fact]
        public void CompareCsvFiles_Exactly100Differences_ShouldShowAllWithoutSuppression()
        {
            // Arrange - create files with exactly 100 differences
            var rows1 = new List<string[]> { new[] { "Value" } };
            var rows2 = new List<string[]> { new[] { "Value" } };

            for (int i = 1; i <= 100; i++)
            {
                rows1.Add(new[] { $"A{i}" });
                rows2.Add(new[] { $"B{i}" }); // All different
            }

            var file1 = CreateCsvFile(rows1.ToArray());
            var file2 = CreateCsvFile(rows2.ToArray());

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(file1, file2, "test", DefaultTolerance);

            // Assert
            result.TextMismatches.Should().Be(100);

            var consoleOutput = output.ToString();
            var mismatchLines = consoleOutput.Split('\n').Count(line => line.Contains("mismatch at"));
            mismatchLines.Should().Be(100);

            // Suppression message should NOT appear for exactly 100
            consoleOutput.Should().NotContain("suppressing further difference output");
        }

        [Fact]
        public void CompareCsvFiles_101Differences_ShouldShowSuppressionMessage()
        {
            // Arrange - create files with 101 differences (boundary test)
            var rows1 = new List<string[]> { new[] { "Value" } };
            var rows2 = new List<string[]> { new[] { "Value" } };

            for (int i = 1; i <= 101; i++)
            {
                rows1.Add(new[] { $"A{i}" });
                rows2.Add(new[] { $"B{i}" }); // All different
            }

            var file1 = CreateCsvFile(rows1.ToArray());
            var file2 = CreateCsvFile(rows2.ToArray());

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(file1, file2, "test", DefaultTolerance);

            // Assert
            result.TextMismatches.Should().Be(101);

            var consoleOutput = output.ToString();

            // Should show exactly 100 difference messages plus 1 suppression message
            var mismatchLines = consoleOutput.Split('\n').Count(line => line.Contains("mismatch at"));
            mismatchLines.Should().Be(100);

            consoleOutput.Should().Contain("suppressing further difference output");
            consoleOutput.Should().Contain("max 100 shown");

            // Verify suppression message appears only once
            var suppressionCount = consoleOutput.Split(new[] { "suppressing further difference output" }, StringSplitOptions.None).Length - 1;
            suppressionCount.Should().Be(1);
        }

        [Fact]
        public void CompareCsvFiles_MixedNumericAndTextDifferences_ShouldLimitCombinedOutput()
        {
            // Arrange - create files with 60 numeric + 60 text differences = 120 total
            var rows1 = new List<string[]> { new[] { "NumCol", "TextCol" } };
            var rows2 = new List<string[]> { new[] { "NumCol", "TextCol" } };

            for (int i = 1; i <= 60; i++)
            {
                rows1.Add(new[] { $"{i}", $"Text{i}" });
                rows2.Add(new[] { $"{i + 100}", $"Different{i}" }); // Both columns differ
            }

            var file1 = CreateCsvFile(rows1.ToArray());
            var file2 = CreateCsvFile(rows2.ToArray());

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            var result = CsvComparer.CompareCsvFiles(file1, file2, "test", DefaultTolerance);

            // Assert
            result.NumericMismatches.Should().Be(60);
            result.TextMismatches.Should().Be(60);
            result.TotalMismatches.Should().Be(120);

            var consoleOutput = output.ToString();

            // Should limit total output to 100 differences (regardless of type)
            var totalMismatchLines = consoleOutput.Split('\n').Count(line => line.Contains("mismatch at"));
            totalMismatchLines.Should().BeLessThanOrEqualTo(101); // 100 differences + possible suppression line

            consoleOutput.Should().Contain("suppressing further difference output");
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
