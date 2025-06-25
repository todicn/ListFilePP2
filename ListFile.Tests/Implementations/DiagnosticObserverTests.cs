using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ListFile.Core.Implementations;
using ListFile.Core.Interfaces;
using ListFile.Core.Services;

namespace ListFile.Tests.Implementations;

public class DiagnosticObserverTests
{
    [Fact]
    public void DiagnosticSubject_Attach_AddsObserver()
    {
        // Arrange
        var subject = new DiagnosticSubject();
        var observer = new Mock<IDiagnosticObserver>().Object;

        // Act
        subject.Attach(observer);

        // Assert
        Assert.Equal(1, subject.ObserverCount);
    }

    [Fact]
    public void DiagnosticSubject_Attach_SameObserverTwice_AddsOnlyOnce()
    {
        // Arrange
        var subject = new DiagnosticSubject();
        var observer = new Mock<IDiagnosticObserver>().Object;

        // Act
        subject.Attach(observer);
        subject.Attach(observer);

        // Assert
        Assert.Equal(1, subject.ObserverCount);
    }

    [Fact]
    public void DiagnosticSubject_Detach_RemovesObserver()
    {
        // Arrange
        var subject = new DiagnosticSubject();
        var observer = new Mock<IDiagnosticObserver>().Object;
        subject.Attach(observer);

        // Act
        subject.Detach(observer);

        // Assert
        Assert.Equal(0, subject.ObserverCount);
    }

    [Fact]
    public void DiagnosticSubject_Notify_CallsAllObservers()
    {
        // Arrange
        var subject = new DiagnosticSubject();
        var observer1 = new Mock<IDiagnosticObserver>();
        var observer2 = new Mock<IDiagnosticObserver>();
        var diagnosticEvent = DiagnosticEvent.FileOperationStarted("Test", "test.txt", "Read", 10);

        subject.Attach(observer1.Object);
        subject.Attach(observer2.Object);

        // Act
        subject.Notify(diagnosticEvent);

        // Assert
        observer1.Verify(o => o.OnDiagnosticEvent(diagnosticEvent), Times.Once);
        observer2.Verify(o => o.OnDiagnosticEvent(diagnosticEvent), Times.Once);
    }

    [Fact]
    public void DiagnosticSubject_Notify_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var subject = new DiagnosticSubject();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => subject.Notify(null!));
    }

    [Fact]
    public void DiagnosticSubject_Attach_WithNullObserver_ThrowsArgumentNullException()
    {
        // Arrange
        var subject = new DiagnosticSubject();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => subject.Attach(null!));
    }

    [Fact]
    public void DiagnosticEvent_FileOperationStarted_CreatesCorrectEvent()
    {
        // Act
        var diagnosticEvent = DiagnosticEvent.FileOperationStarted("FileReader", "test.txt", "ReadLastLines", 10);

        // Assert
        Assert.Equal(DiagnosticEventType.FileOperationStarted, diagnosticEvent.EventType);
        Assert.Equal("FileReader", diagnosticEvent.Source);
        Assert.Equal("test.txt", diagnosticEvent.Data["FilePath"]);
        Assert.Equal("ReadLastLines", diagnosticEvent.Data["Operation"]);
        Assert.Equal(10, diagnosticEvent.Data["LineCount"]);
    }

    [Fact]
    public void DiagnosticEvent_FileOperationCompleted_CreatesCorrectEvent()
    {
        // Act
        var diagnosticEvent = DiagnosticEvent.FileOperationCompleted("FileReader", "test.txt", "ReadLastLines", 150, 10, 1024);

        // Assert
        Assert.Equal(DiagnosticEventType.FileOperationCompleted, diagnosticEvent.EventType);
        Assert.Equal("FileReader", diagnosticEvent.Source);
        Assert.Equal("test.txt", diagnosticEvent.Data["FilePath"]);
        Assert.Equal("ReadLastLines", diagnosticEvent.Data["Operation"]);
        Assert.Equal(150L, diagnosticEvent.Data["ElapsedMilliseconds"]);
        Assert.Equal(10, diagnosticEvent.Data["LinesRead"]);
        Assert.Equal(1024L, diagnosticEvent.Data["FileSize"]);
    }

    [Fact]
    public void PerformanceDiagnosticObserver_HandleFileOperationCompleted_LogsCorrectly()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<PerformanceDiagnosticObserver>>();
        var observer = new PerformanceDiagnosticObserver(mockLogger.Object, enableConsoleOutput: false);
        var diagnosticEvent = DiagnosticEvent.FileOperationCompleted("FileReader", "test.txt", "ReadLastLines", 150, 10, 1024);

        // Act
        observer.OnDiagnosticEvent(diagnosticEvent);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("File operation completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void MonitoringDiagnosticObserver_HandleMonitoringStarted_TracksStatistics()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<MonitoringDiagnosticObserver>>();
        var observer = new MonitoringDiagnosticObserver(mockLogger.Object);
        var diagnosticEvent = DiagnosticEvent.MonitoringStarted("FileMonitor", "test.txt", 10);

        // Act
        observer.OnDiagnosticEvent(diagnosticEvent);

        // Assert
        var stats = observer.GetMonitoringStatistics();
        Assert.Single(stats);
        Assert.Contains("test.txt", stats.Keys);
        Assert.Equal(10, stats["test.txt"].LineCount);
    }

    [Fact]
    public void MonitoringDiagnosticObserver_HandleFileChangeDetected_UpdatesChangeCount()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<MonitoringDiagnosticObserver>>();
        var observer = new MonitoringDiagnosticObserver(mockLogger.Object);
        var startEvent = DiagnosticEvent.MonitoringStarted("FileMonitor", "test.txt", 10);
        var changeEvent = DiagnosticEvent.FileChangeDetected("FileMonitor", "test.txt", "Modified", 5);

        // Act
        observer.OnDiagnosticEvent(startEvent);
        observer.OnDiagnosticEvent(changeEvent);

        // Assert
        var stats = observer.GetMonitoringStatistics();
        Assert.Equal(1, stats["test.txt"].ChangeCount);
        Assert.NotNull(stats["test.txt"].LastChangeTime);
    }

    [Fact]
    public void DiagnosticEvent_StrategySelected_CreatesCorrectEvent()
    {
        // Act
        var diagnosticEvent = DiagnosticEvent.StrategySelected("FileReader", "SmallFileStrategy", 1024, 2048);

        // Assert
        Assert.Equal(DiagnosticEventType.StrategySelected, diagnosticEvent.EventType);
        Assert.Equal("FileReader", diagnosticEvent.Source);
        Assert.Equal("SmallFileStrategy", diagnosticEvent.Data["StrategyName"]);
        Assert.Equal(1024L, diagnosticEvent.Data["FileSize"]);
        Assert.Equal(2048L, diagnosticEvent.Data["Threshold"]);
    }
} 