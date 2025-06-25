using System.Text;
using ListFile.Core.Configuration;
using ListFile.Core.Implementations;
using Xunit;

namespace ListFile.Tests.Implementations;

/// <summary>
/// Tests for the SmallFileReadingStrategy class.
/// </summary>
public class SmallFileReadingStrategyTests : IDisposable
{
    private readonly List<string> testFiles;
    private readonly SmallFileReadingStrategy strategy;
    private readonly FileReaderOptions options;

    public SmallFileReadingStrategyTests()
    {
        testFiles = new List<string>();
        strategy = new SmallFileReadingStrategy();
        options = new FileReaderOptions
        {
            SmallFileThresholdBytes = 1024 // 1KB
        };
    }

    public void Dispose()
    {
        // Clean up test files
        foreach (var file in testFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    private string CreateTestFile(string fileName, string[] lines)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        File.WriteAllLines(uniqueFileName, lines);
        testFiles.Add(uniqueFileName);
        return uniqueFileName;
    }

    [Fact]
    public void CanHandle_SmallFile_ReturnsTrue()
    {
        // Arrange
        var testFile = CreateTestFile("small.txt", new[] { "line 1", "line 2" });
        var fileInfo = new FileInfo(testFile);

        // Act
        var canHandle = strategy.CanHandle(fileInfo, options);

        // Assert
        Assert.True(canHandle);
    }

    [Fact]
    public void CanHandle_LargeFile_ReturnsFalse()
    {
        // Arrange
        var largeContent = new string('x', 2000); // 2KB content
        var testFile = CreateTestFile("large.txt", new[] { largeContent });
        var fileInfo = new FileInfo(testFile);

        // Act
        var canHandle = strategy.CanHandle(fileInfo, options);

        // Assert
        Assert.False(canHandle);
    }

    [Fact]
    public void ReadLastLines_SmallFile_ReturnsCorrectLines()
    {
        // Arrange
        var lines = new[] { "Line 1", "Line 2", "Line 3", "Line 4", "Line 5" };
        var testFile = CreateTestFile("test.txt", lines);

        // Act
        var result = strategy.ReadLastLines(testFile, 3, options).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(3, result[0].LineNumber);
        Assert.Equal("Line 3", result[0].Content);
        Assert.Equal(4, result[1].LineNumber);
        Assert.Equal("Line 4", result[1].Content);
        Assert.Equal(5, result[2].LineNumber);
        Assert.Equal("Line 5", result[2].Content);
    }

    [Fact]
    public void ReadLastLines_EmptyFile_ReturnsEmptyCollection()
    {
        // Arrange
        var testFile = CreateTestFile("empty.txt", Array.Empty<string>());

        // Act
        var result = strategy.ReadLastLines(testFile, 10, options);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ReadLastLines_RequestMoreLinesThanAvailable_ReturnsAllLines()
    {
        // Arrange
        var lines = new[] { "Line 1", "Line 2" };
        var testFile = CreateTestFile("short.txt", lines);

        // Act
        var result = strategy.ReadLastLines(testFile, 10, options).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].LineNumber);
        Assert.Equal("Line 1", result[0].Content);
        Assert.Equal(2, result[1].LineNumber);
        Assert.Equal("Line 2", result[1].Content);
    }
} 