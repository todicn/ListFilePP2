using ListFile.Core.Implementations;
using Xunit;

namespace ListFile.Tests.Implementations;

/// <summary>
/// Tests for the FileLine class.
/// </summary>
public class FileLineTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesFileLine()
    {
        // Arrange
        int lineNumber = 5;
        string content = "This is a test line";

        // Act
        var fileLine = new FileLine(lineNumber, content);

        // Assert
        Assert.Equal(lineNumber, fileLine.LineNumber);
        Assert.Equal(content, fileLine.Content);
    }

    [Fact]
    public void Constructor_LineNumberLessThanOne_ThrowsArgumentException()
    {
        // Arrange
        int lineNumber = 0;
        string content = "Test content";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FileLine(lineNumber, content));
    }

    [Fact]
    public void Constructor_NullContent_ThrowsArgumentNullException()
    {
        // Arrange
        int lineNumber = 1;
        string? content = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileLine(lineNumber, content!));
    }

    [Fact]
    public void ToString_ValidFileLine_ReturnsCorrectFormat()
    {
        // Arrange
        var fileLine = new FileLine(10, "Sample line content");
        var expected = "10: Sample line content";

        // Act
        var result = fileLine.ToString();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Equals_SameLineNumberAndContent_ReturnsTrue()
    {
        // Arrange
        var fileLine1 = new FileLine(5, "Test content");
        var fileLine2 = new FileLine(5, "Test content");

        // Act
        var result = fileLine1.Equals(fileLine2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_DifferentLineNumber_ReturnsFalse()
    {
        // Arrange
        var fileLine1 = new FileLine(5, "Test content");
        var fileLine2 = new FileLine(6, "Test content");

        // Act
        var result = fileLine1.Equals(fileLine2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_DifferentContent_ReturnsFalse()
    {
        // Arrange
        var fileLine1 = new FileLine(5, "Test content 1");
        var fileLine2 = new FileLine(5, "Test content 2");

        // Act
        var result = fileLine1.Equals(fileLine2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_NullObject_ReturnsFalse()
    {
        // Arrange
        var fileLine = new FileLine(5, "Test content");

        // Act
        var result = fileLine.Equals(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetHashCode_SameLineNumberAndContent_ReturnsSameHashCode()
    {
        // Arrange
        var fileLine1 = new FileLine(5, "Test content");
        var fileLine2 = new FileLine(5, "Test content");

        // Act
        var hashCode1 = fileLine1.GetHashCode();
        var hashCode2 = fileLine2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_DifferentLineNumberOrContent_ReturnsDifferentHashCode()
    {
        // Arrange
        var fileLine1 = new FileLine(5, "Test content");
        var fileLine2 = new FileLine(6, "Test content");
        var fileLine3 = new FileLine(5, "Different content");

        // Act
        var hashCode1 = fileLine1.GetHashCode();
        var hashCode2 = fileLine2.GetHashCode();
        var hashCode3 = fileLine3.GetHashCode();

        // Assert
        Assert.NotEqual(hashCode1, hashCode2);
        Assert.NotEqual(hashCode1, hashCode3);
    }
} 