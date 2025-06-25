using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ListFile.Core.Implementations;
using ListFile.Core.Interfaces;

namespace ListFile.Tests.Implementations;

public class FileMonitorTests : IDisposable
{
    private readonly Mock<IFileReader> mockFileReader;
    private readonly Mock<ILogger<FileMonitor>> mockLogger;
    private readonly FileMonitor fileMonitor;
    private readonly string testDirectory;
    private readonly string testFilePath;

    public FileMonitorTests()
    {
        mockFileReader = new Mock<IFileReader>();
        mockLogger = new Mock<ILogger<FileMonitor>>();
        fileMonitor = new FileMonitor(mockFileReader.Object, mockLogger.Object);
        
        // Create a temporary directory and file for testing
        testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);
        testFilePath = Path.Combine(testDirectory, "test.txt");
        File.WriteAllText(testFilePath, "Initial content\nLine 2\nLine 3\n");
    }

    public void Dispose()
    {
        fileMonitor?.Dispose();
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, true);
        }
    }

    [Fact]
    public void Constructor_WithNullFileReader_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileMonitor(null!, mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_DoesNotThrow()
    {
        // Act & Assert
        var monitor = new FileMonitor(mockFileReader.Object, null);
        Assert.NotNull(monitor);
        monitor.Dispose();
    }

    [Fact]
    public void StartMonitoring_WithNullFilePath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => fileMonitor.StartMonitoring(null!));
    }

    [Fact]
    public void StartMonitoring_WithEmptyFilePath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => fileMonitor.StartMonitoring(""));
    }

    [Fact]
    public void StartMonitoring_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(testDirectory, "nonexistent.txt");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => fileMonitor.StartMonitoring(nonExistentPath));
    }

    [Fact]
    public void StartMonitoring_WithInvalidLineCount_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => fileMonitor.StartMonitoring(testFilePath, 0));
        Assert.Throws<ArgumentException>(() => fileMonitor.StartMonitoring(testFilePath, -1));
    }

    [Fact]
    public void StartMonitoring_WithValidFile_SetsProperties()
    {
        // Arrange
        var expectedLines = new List<IFileLine>
        {
            new FileLine(1, "Line 1"),
            new FileLine(2, "Line 2")
        };
        mockFileReader.Setup(x => x.ReadLastLines(testFilePath, 10))
                     .Returns(expectedLines);

        // Act
        fileMonitor.StartMonitoring(testFilePath, 10);

        // Assert
        Assert.True(fileMonitor.IsMonitoring);
        Assert.Equal(testFilePath, fileMonitor.MonitoredFilePath);
        mockFileReader.Verify(x => x.ReadLastLines(testFilePath, 10), Times.Once);
    }

    [Fact]
    public void StartMonitoring_WhenAlreadyMonitoring_StopsCurrentAndStartsNew()
    {
        // Arrange
        var firstFile = testFilePath;
        var secondFile = Path.Combine(testDirectory, "test2.txt");
        File.WriteAllText(secondFile, "Second file content\n");

        var expectedLines = new List<IFileLine>
        {
            new FileLine(1, "Line 1")
        };
        mockFileReader.Setup(x => x.ReadLastLines(It.IsAny<string>(), It.IsAny<int>()))
                     .Returns(expectedLines);

        // Act
        fileMonitor.StartMonitoring(firstFile);
        fileMonitor.StartMonitoring(secondFile);

        // Assert
        Assert.True(fileMonitor.IsMonitoring);
        Assert.Equal(secondFile, fileMonitor.MonitoredFilePath);
    }

    [Fact]
    public void StopMonitoring_WhenMonitoring_StopsMonitoring()
    {
        // Arrange
        var expectedLines = new List<IFileLine>
        {
            new FileLine(1, "Line 1")
        };
        mockFileReader.Setup(x => x.ReadLastLines(testFilePath, 10))
                     .Returns(expectedLines);

        fileMonitor.StartMonitoring(testFilePath);

        // Act
        fileMonitor.StopMonitoring();

        // Assert
        Assert.False(fileMonitor.IsMonitoring);
        Assert.Null(fileMonitor.MonitoredFilePath);
    }

    [Fact]
    public void StopMonitoring_WhenNotMonitoring_DoesNotThrow()
    {
        // Act & Assert
        fileMonitor.StopMonitoring(); // Should not throw
        Assert.False(fileMonitor.IsMonitoring);
    }

    [Fact]
    public async Task FileChanged_Event_RaisedOnFileModification()
    {
        // Arrange
        var expectedLines = new List<IFileLine>
        {
            new FileLine(1, "New line 1"),
            new FileLine(2, "New line 2")
        };

        mockFileReader.Setup(x => x.ReadLastLines(testFilePath, 10))
                     .Returns(expectedLines);

        FileChangedEventArgs? capturedEventArgs = null;
        fileMonitor.FileChanged += (sender, args) => capturedEventArgs = args;

        fileMonitor.StartMonitoring(testFilePath);

        // Act - Modify the file
        await File.AppendAllTextAsync(testFilePath, "New content\n");
        
        // Wait for file system watcher to detect the change
        await Task.Delay(200);

        // Assert
        Assert.NotNull(capturedEventArgs);
        Assert.Equal(testFilePath, capturedEventArgs.FilePath);
        Assert.Equal(FileChangeType.Modified, capturedEventArgs.ChangeType);
        Assert.Equal(expectedLines, capturedEventArgs.Lines);
    }

    [Fact]
    public void Dispose_StopsMonitoring()
    {
        // Arrange
        var expectedLines = new List<IFileLine>
        {
            new FileLine(1, "Line 1")
        };
        mockFileReader.Setup(x => x.ReadLastLines(testFilePath, 10))
                     .Returns(expectedLines);

        fileMonitor.StartMonitoring(testFilePath);

        // Act
        fileMonitor.Dispose();

        // Assert
        Assert.False(fileMonitor.IsMonitoring);
        Assert.Null(fileMonitor.MonitoredFilePath);
    }

    [Fact]
    public void FileChangedEventArgs_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var filePath = "/path/to/file.txt";
        var lines = new List<IFileLine> { new FileLine(1, "Test line") };
        var changeType = FileChangeType.Modified;
        var beforeTime = DateTime.UtcNow;

        // Act
        var eventArgs = new FileChangedEventArgs(filePath, lines, changeType);
        var afterTime = DateTime.UtcNow;

        // Assert
        Assert.Equal(filePath, eventArgs.FilePath);
        Assert.Equal(lines, eventArgs.Lines);
        Assert.Equal(changeType, eventArgs.ChangeType);
        Assert.True(eventArgs.Timestamp >= beforeTime && eventArgs.Timestamp <= afterTime);
    }

    [Fact]
    public void FileChangedEventArgs_Constructor_WithNullFilePath_ThrowsArgumentNullException()
    {
        // Arrange
        var lines = new List<IFileLine> { new FileLine(1, "Test line") };
        var changeType = FileChangeType.Modified;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileChangedEventArgs(null!, lines, changeType));
    }

    [Fact]
    public void FileChangedEventArgs_Constructor_WithNullLines_ThrowsArgumentNullException()
    {
        // Arrange
        var filePath = "/path/to/file.txt";
        var changeType = FileChangeType.Modified;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileChangedEventArgs(filePath, null!, changeType));
    }
} 