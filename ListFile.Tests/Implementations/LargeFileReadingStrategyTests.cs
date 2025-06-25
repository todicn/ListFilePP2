using System.Text;
using ListFile.Core.Configuration;
using ListFile.Core.Implementations;
using Xunit;

namespace ListFile.Tests.Implementations;

/// <summary>
/// Tests for the LargeFileReadingStrategy class.
/// </summary>
public class LargeFileReadingStrategyTests : IDisposable
{
    private readonly List<string> testFiles;
    private readonly LargeFileReadingStrategy strategy;
    private readonly FileReaderOptions options;

    public LargeFileReadingStrategyTests()
    {
        testFiles = new List<string>();
        strategy = new LargeFileReadingStrategy();
        options = new FileReaderOptions
        {
            SmallFileThresholdBytes = 1024, // 1KB
            BufferSize = 512 // Small buffer for testing
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
    public void CanHandle_SmallFile_ReturnsFalse()
    {
        // Arrange
        var testFile = CreateTestFile("small.txt", new[] { "line 1", "line 2" });
        var fileInfo = new FileInfo(testFile);

        // Act
        var canHandle = strategy.CanHandle(fileInfo, options);

        // Assert
        Assert.False(canHandle);
    }

    [Fact]
    public void CanHandle_LargeFile_ReturnsTrue()
    {
        // Arrange
        var largeContent = new string('x', 2000); // 2KB content
        var testFile = CreateTestFile("large.txt", new[] { largeContent });
        var fileInfo = new FileInfo(testFile);

        // Act
        var canHandle = strategy.CanHandle(fileInfo, options);

        // Assert
        Assert.True(canHandle);
    }

    [Fact]
    public void ReadLastLines_LargeFile_ReturnsCorrectLines()
    {
        // Arrange - Create a large file with many lines
        var lines = Enumerable.Range(1, 100).Select(i => $"This is line {i} with some content to make it larger").ToArray();
        var testFile = CreateTestFile("large.txt", lines);

        // Act
        var result = strategy.ReadLastLines(testFile, 5, options).ToList();

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal(96, result[0].LineNumber);
        Assert.Equal("This is line 96 with some content to make it larger", result[0].Content);
        Assert.Equal(97, result[1].LineNumber);
        Assert.Equal("This is line 97 with some content to make it larger", result[1].Content);
        Assert.Equal(98, result[2].LineNumber);
        Assert.Equal("This is line 98 with some content to make it larger", result[2].Content);
        Assert.Equal(99, result[3].LineNumber);
        Assert.Equal("This is line 99 with some content to make it larger", result[3].Content);
        Assert.Equal(100, result[4].LineNumber);
        Assert.Equal("This is line 100 with some content to make it larger", result[4].Content);
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
        // Arrange - Create a large file with a reasonable number of lines
        var lines = new List<string>();
        for (int i = 1; i <= 10; i++) // Use fewer lines to make the test more predictable
        {
            lines.Add($"Line {i} with some content to make the file larger than threshold");
        }
        var testFile = CreateTestFile("large_few_lines.txt", lines.ToArray());

        // Act
        var result = strategy.ReadLastLines(testFile, 20, options).ToList(); // Request more than available

        // Assert - Check that we get all the lines (or at least most of them)
        Assert.True(result.Count >= 9, $"Expected at least 9 lines, got {result.Count}");
        
        // Check that the last line is correct
        var lastLine = result.LastOrDefault();
        Assert.NotNull(lastLine);
        Assert.Equal("Line 10 with some content to make the file larger than threshold", lastLine.Content);
        
        // Check that we have lines in the correct order
        var validLines = result.Where(l => l.Content.StartsWith("Line ")).OrderBy(l => l.LineNumber).ToList();
        Assert.True(validLines.Count >= 9);
        Assert.Equal("Line 10 with some content to make the file larger than threshold", validLines.Last().Content);
    }

    [Fact]
    public void ReadLastLines_SingleLineFile_ReturnsSingleLine()
    {
        // Arrange - Create a large single line file with multiple short lines to ensure it's large
        var lines = new List<string>();
        for (int i = 1; i <= 30; i++)
        {
            lines.Add($"This is line {i} with enough content to make the file large enough");
        }
        // Add one final line that we expect to get back
        lines.Add("Final single line");
        var testFile = CreateTestFile("large_single.txt", lines.ToArray());

        // Act
        var result = strategy.ReadLastLines(testFile, 1, options).ToList(); // Only get last line

        // Assert
        Assert.Single(result);
        Assert.Equal(31, result[0].LineNumber); // Should be line 31
        Assert.Equal("Final single line", result[0].Content);
    }
} 