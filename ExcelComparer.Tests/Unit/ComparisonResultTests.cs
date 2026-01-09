using ExcelComparer;
using FluentAssertions;
using System.IO;
using Xunit;

namespace ExcelComparer.Tests.Unit
{
    public class ComparisonResultTests
    {
        [Fact]
        public void NewInstance_ShouldHaveZeroValues()
        {
            // Arrange & Act
            var result = new ComparisonResult();

            // Assert
            result.TotalCellsCompared.Should().Be(0);
            result.NumericMismatches.Should().Be(0);
            result.TextMismatches.Should().Be(0);
            result.SheetsCompared.Should().Be(0);
        }

        [Theory]
        [InlineData(5, 3, 8)]
        [InlineData(0, 0, 0)]
        [InlineData(10, 0, 10)]
        [InlineData(0, 7, 7)]
        public void TotalMismatches_ShouldReturnSumOfNumericAndText(int numeric, int text, int expected)
        {
            // Arrange
            var result = new ComparisonResult
            {
                NumericMismatches = numeric,
                TextMismatches = text
            };

            // Act
            var total = result.TotalMismatches;

            // Assert
            total.Should().Be(expected);
        }

        [Fact]
        public void PrintSummary_WithNoMismatches_ShouldDisplaySuccessMessage()
        {
            // Arrange
            var result = new ComparisonResult
            {
                SheetsCompared = 2,
                TotalCellsCompared = 100,
                NumericMismatches = 0,
                TextMismatches = 0
            };

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            result.PrintSummary();

            // Assert
            var consoleOutput = output.ToString();
            consoleOutput.Should().Contain("Comparison Summary");
            consoleOutput.Should().Contain("Sheets compared: 2");
            consoleOutput.Should().Contain("Total cells compared: 100");
            consoleOutput.Should().Contain("No differences found");
        }

        [Fact]
        public void PrintSummary_WithMismatches_ShouldDisplayWarning()
        {
            // Arrange
            var result = new ComparisonResult
            {
                SheetsCompared = 1,
                TotalCellsCompared = 50,
                NumericMismatches = 3,
                TextMismatches = 2
            };

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            result.PrintSummary();

            // Assert
            var consoleOutput = output.ToString();
            consoleOutput.Should().Contain("Comparison Summary");
            consoleOutput.Should().Contain("Total differences found: 5");
            consoleOutput.Should().Contain("Numeric mismatches: 3");
            consoleOutput.Should().Contain("Text mismatches: 2");
        }

        [Fact]
        public void Properties_ShouldBeMutable()
        {
            // Arrange
            var result = new ComparisonResult();

            // Act
            result.TotalCellsCompared = 100;
            result.NumericMismatches = 5;
            result.TextMismatches = 3;
            result.SheetsCompared = 2;

            // Assert
            result.TotalCellsCompared.Should().Be(100);
            result.NumericMismatches.Should().Be(5);
            result.TextMismatches.Should().Be(3);
            result.SheetsCompared.Should().Be(2);
            result.TotalMismatches.Should().Be(8);
        }

        [Fact]
        public void TotalCellsCompared_ShouldSupportLargeValues()
        {
            // Arrange
            var result = new ComparisonResult();

            // Act - Set to a value larger than int.MaxValue
            result.TotalCellsCompared = 3_000_000_000L;

            // Assert
            result.TotalCellsCompared.Should().Be(3_000_000_000L);
        }
    }
}
