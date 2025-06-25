using Microsoft.Extensions.Options;
using ListFile.Core.Configuration;
using ListFile.Core.Implementations;
using ListFile.Core.Interfaces;
using Xunit;

namespace ListFile.Tests.Implementations;

/// <summary>
/// Tests for the FileReader class.
/// </summary>
public class FileReaderTests : IDisposable
{
    private readonly List<string> testFiles;
    private readonly FileReader fileReader;

    public FileReaderTests()
    {
        testFiles = new List<string>();
        var options = Options.Create(new FileReaderOptions
        {
            EnablePerformanceLogging = false,
            SmallFileThresholdBytes = 1024 // 1KB for tests
        });
        fileReader = new FileReader(options);
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
        File.WriteAllLines(fileName, lines);
        testFiles.Add(fileName);
        return fileName;
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileReader(null!));
    }

    [Fact]
    public void ReadLastLines_NullFilePath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => fileReader.ReadLastLines(null!));
        Assert.Throws<ArgumentNullException>(() => fileReader.ReadLastLines(""));
        Assert.Throws<ArgumentNullException>(() => fileReader.ReadLastLines("  "));
    }

    [Fact]
    public void ReadLastLines_InvalidLineCount_ThrowsArgumentException()
    {
        // Arrange
        var testFile = CreateTestFile("test.txt", new[] { "line 1" });

        // Act & Assert
        Assert.Throws<ArgumentException>(() => fileReader.ReadLastLines(testFile, 0));
        Assert.Throws<ArgumentException>(() => fileReader.ReadLastLines(testFile, -1));
    }

    [Fact]
    public void ReadLastLines_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => fileReader.ReadLastLines("non_existent_file.txt"));
    }

    [Fact]
    public void ReadLastLines_EmptyFile_ReturnsEmptyCollection()
    {
        // Arrange
        var testFile = CreateTestFile("empty.txt", Array.Empty<string>());

        // Act
        var result = fileReader.ReadLastLines(testFile, 10);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ReadLastLines_SingleLineFile_ReturnsSingleLine()
    {
        // Arrange
        var testFile = CreateTestFile("single.txt", new[] { "Only line" });

        // Act
        var result = fileReader.ReadLastLines(testFile, 10).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].LineNumber);
        Assert.Equal("Only line", result[0].Content);
    }

    [Fact]
    public void ReadLastLines_SmallFile_ReturnsCorrectLastLines()
    {
        // Arrange
        var lines = new[] { "Line 1", "Line 2", "Line 3", "Line 4", "Line 5" };
        var testFile = CreateTestFile("small.txt", lines);

        // Act
        var result = fileReader.ReadLastLines(testFile, 3).ToList();

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
    public void ReadLastLines_RequestMoreLinesThanAvailable_ReturnsAllLines()
    {
        // Arrange
        var lines = new[] { "Line 1", "Line 2", "Line 3" };
        var testFile = CreateTestFile("short.txt", lines);

        // Act
        var result = fileReader.ReadLastLines(testFile, 10).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].LineNumber);
        Assert.Equal("Line 1", result[0].Content);
        Assert.Equal(2, result[1].LineNumber);
        Assert.Equal("Line 2", result[1].Content);
        Assert.Equal(3, result[2].LineNumber);
        Assert.Equal("Line 3", result[2].Content);
    }

    [Fact]
    public void ReadLastLines_DefaultLineCount_ReturnsLast10Lines()
    {
        // Arrange
        var lines = Enumerable.Range(1, 15).Select(i => $"Line {i}").ToArray();
        var testFile = CreateTestFile("default_count.txt", lines);

        // Act
        var result = fileReader.ReadLastLines(testFile).ToList();

        // Assert
        Assert.Equal(10, result.Count);
        Assert.Equal(6, result[0].LineNumber);
        Assert.Equal("Line 6", result[0].Content);
        Assert.Equal(15, result[9].LineNumber);
        Assert.Equal("Line 15", result[9].Content);
    }

    [Fact]
    public async Task ReadLastLinesAsync_ValidFile_ReturnsCorrectLines()
    {
        // Arrange
        var lines = new[] { "Line 1", "Line 2", "Line 3", "Line 4", "Line 5" };
        var testFile = CreateTestFile("async_test.txt", lines);

        // Act
        var result = (await fileReader.ReadLastLinesAsync(testFile, 2)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(4, result[0].LineNumber);
        Assert.Equal("Line 4", result[0].Content);
        Assert.Equal(5, result[1].LineNumber);
        Assert.Equal("Line 5", result[1].Content);
    }

    [Fact]
    public async Task ReadLastLinesAsync_NullFilePath_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => fileReader.ReadLastLinesAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => fileReader.ReadLastLinesAsync(""));
        await Assert.ThrowsAsync<ArgumentNullException>(() => fileReader.ReadLastLinesAsync("  "));
    }

    [Fact]
    public void ReadLastLines_LinesWithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var lines = new[] { 
            "Line with «special» characters", 
            "Line with\ttabs", 
            "Line with spaces   ", 
            "Line with 数字 and símbolos" 
        };
        var testFile = CreateTestFile("special_chars.txt", lines);

        // Act
        var result = fileReader.ReadLastLines(testFile, 2).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(3, result[0].LineNumber);
        Assert.Equal("Line with spaces   ", result[0].Content);
        Assert.Equal(4, result[1].LineNumber);
        Assert.Equal("Line with 数字 and símbolos", result[1].Content);
    }

    [Fact]
    public void ReadLastLines_LargeFile_PerformanceTest()
    {
        // Arrange
        var lines = Enumerable.Range(1, 1000).Select(i => $"Line {i} with some content to make it longer").ToArray();
        var testFile = CreateTestFile("large_test.txt", lines);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = fileReader.ReadLastLines(testFile, 5).ToList();
        stopwatch.Stop();

        // Assert
        Assert.Equal(5, result.Count);
        Assert.Equal(996, result[0].LineNumber);
        Assert.Equal(1000, result[4].LineNumber);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Performance test should complete within 1 second");
    }
} 