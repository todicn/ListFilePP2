using ListFile.Core.Configuration;
using ListFile.Core.Implementations;
using ListFile.Core.Interfaces;
using ListFile.Core.Services;
using Xunit;

namespace ListFile.Tests.Services;

/// <summary>
/// Tests for the FileReadingStrategySelector class.
/// </summary>
public class FileReadingStrategySelectorTests : IDisposable
{
    private readonly List<string> testFiles;
    private readonly FileReadingStrategySelector selector;
    private readonly FileReaderOptions options;

    public FileReadingStrategySelectorTests()
    {
        testFiles = new List<string>();
        var strategies = new List<IFileReadingStrategy>
        {
            new SmallFileReadingStrategy(),
            new LargeFileReadingStrategy()
        };
        selector = new FileReadingStrategySelector(strategies);
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
    public void Constructor_NullStrategies_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileReadingStrategySelector(null!));
    }

    [Fact]
    public void SelectStrategy_SmallFile_ReturnsSmallFileStrategy()
    {
        // Arrange
        var testFile = CreateTestFile("small.txt", new[] { "line 1", "line 2" });
        var fileInfo = new FileInfo(testFile);

        // Act
        var strategy = selector.SelectStrategy(fileInfo, options);

        // Assert
        Assert.IsType<SmallFileReadingStrategy>(strategy);
    }

    [Fact]
    public void SelectStrategy_LargeFile_ReturnsLargeFileStrategy()
    {
        // Arrange
        var largeContent = new string('x', 2000); // 2KB content
        var testFile = CreateTestFile("large.txt", new[] { largeContent });
        var fileInfo = new FileInfo(testFile);

        // Act
        var strategy = selector.SelectStrategy(fileInfo, options);

        // Assert
        Assert.IsType<LargeFileReadingStrategy>(strategy);
    }

    [Fact]
    public void SelectStrategy_FileAtThreshold_ReturnsSmallFileStrategy()
    {
        // Arrange - Create file exactly at threshold
        var content = new string('x', 1020); // Account for line endings to stay under 1KB
        var testFile = CreateTestFile("threshold.txt", new[] { content });
        var fileInfo = new FileInfo(testFile);

        // Act
        var strategy = selector.SelectStrategy(fileInfo, options);

        // Assert
        Assert.IsType<SmallFileReadingStrategy>(strategy);
    }

    [Fact]
    public void SelectStrategy_NoSuitableStrategy_ThrowsInvalidOperationException()
    {
        // Arrange - Create selector with no strategies
        var emptySelector = new FileReadingStrategySelector(new List<IFileReadingStrategy>());
        var testFile = CreateTestFile("test.txt", new[] { "line 1" });
        var fileInfo = new FileInfo(testFile);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => emptySelector.SelectStrategy(fileInfo, options));
        Assert.Contains("No suitable strategy found", exception.Message);
    }
} 