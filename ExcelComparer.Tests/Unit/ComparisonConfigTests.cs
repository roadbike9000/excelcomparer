using ExcelComparer;
using FluentAssertions;
using Xunit;

namespace ExcelComparer.Tests.Unit
{
    public class ComparisonConfigTests
    {
        [Fact]
        public void DefaultTolerance_ShouldBe1e15()
        {
            // Arrange & Act
            var tolerance = ComparisonConfig.DefaultTolerance;

            // Assert
            tolerance.Should().Be(1e-15);
        }

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultTolerance()
        {
            // Arrange & Act
            var config = new ComparisonConfig();

            // Assert
            config.Tolerance.Should().Be(ComparisonConfig.DefaultTolerance);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1e-10)]
        [InlineData(0.001)]
        [InlineData(1.0)]
        public void Tolerance_ShouldAcceptValidPositiveValues(double tolerance)
        {
            // Arrange
            var config = new ComparisonConfig();

            // Act
            config.Tolerance = tolerance;

            // Assert
            config.Tolerance.Should().Be(tolerance);
        }

        [Fact]
        public void Tolerance_CanBeSetToZero()
        {
            // Arrange
            var config = new ComparisonConfig();

            // Act
            config.Tolerance = 0.0;

            // Assert
            config.Tolerance.Should().Be(0.0);
        }
    }
}
