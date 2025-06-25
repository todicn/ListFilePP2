using ListFile.Core.Interfaces;

namespace ListFile.Core.Implementations;

/// <summary>
/// Represents a diagnostic event that occurred in the system.
/// </summary>
public class DiagnosticEvent : IDiagnosticEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticEvent"/> class.
    /// </summary>
    /// <param name="eventType">The type of diagnostic event.</param>
    /// <param name="source">The source component that generated the event.</param>
    /// <param name="data">Additional event data.</param>
    public DiagnosticEvent(DiagnosticEventType eventType, string source, IDictionary<string, object>? data = null)
    {
        EventType = eventType;
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Data = data ?? new Dictionary<string, object>();
        Timestamp = DateTime.UtcNow;
    }

    /// <inheritdoc />
    public DateTime Timestamp { get; }

    /// <inheritdoc />
    public DiagnosticEventType EventType { get; }

    /// <inheritdoc />
    public string Source { get; }

    /// <inheritdoc />
    public IDictionary<string, object> Data { get; }

    /// <summary>
    /// Creates a file operation started event.
    /// </summary>
    /// <param name="source">The source component.</param>
    /// <param name="filePath">The file path being operated on.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="lineCount">The number of lines requested (if applicable).</param>
    /// <returns>A diagnostic event.</returns>
    public static DiagnosticEvent FileOperationStarted(string source, string filePath, string operation, int? lineCount = null)
    {
        var data = new Dictionary<string, object>
        {
            ["FilePath"] = filePath,
            ["Operation"] = operation
        };
        
        if (lineCount.HasValue)
        {
            data["LineCount"] = lineCount.Value;
        }

        return new DiagnosticEvent(DiagnosticEventType.FileOperationStarted, source, data);
    }

    /// <summary>
    /// Creates a file operation completed event.
    /// </summary>
    /// <param name="source">The source component.</param>
    /// <param name="filePath">The file path that was operated on.</param>
    /// <param name="operation">The operation that was performed.</param>
    /// <param name="elapsedMilliseconds">The time taken for the operation.</param>
    /// <param name="linesRead">The number of lines read (if applicable).</param>
    /// <param name="fileSize">The size of the file (if applicable).</param>
    /// <returns>A diagnostic event.</returns>
    public static DiagnosticEvent FileOperationCompleted(string source, string filePath, string operation, 
        long elapsedMilliseconds, int? linesRead = null, long? fileSize = null)
    {
        var data = new Dictionary<string, object>
        {
            ["FilePath"] = filePath,
            ["Operation"] = operation,
            ["ElapsedMilliseconds"] = elapsedMilliseconds
        };
        
        if (linesRead.HasValue)
        {
            data["LinesRead"] = linesRead.Value;
        }
        
        if (fileSize.HasValue)
        {
            data["FileSize"] = fileSize.Value;
        }

        return new DiagnosticEvent(DiagnosticEventType.FileOperationCompleted, source, data);
    }

    /// <summary>
    /// Creates a file operation failed event.
    /// </summary>
    /// <param name="source">The source component.</param>
    /// <param name="filePath">The file path that failed.</param>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A diagnostic event.</returns>
    public static DiagnosticEvent FileOperationFailed(string source, string filePath, string operation, Exception exception)
    {
        var data = new Dictionary<string, object>
        {
            ["FilePath"] = filePath,
            ["Operation"] = operation,
            ["Exception"] = exception.GetType().Name,
            ["Message"] = exception.Message
        };

        return new DiagnosticEvent(DiagnosticEventType.FileOperationFailed, source, data);
    }

    /// <summary>
    /// Creates a performance metrics event.
    /// </summary>
    /// <param name="source">The source component.</param>
    /// <param name="metrics">The performance metrics.</param>
    /// <returns>A diagnostic event.</returns>
    public static DiagnosticEvent PerformanceMetrics(string source, IDictionary<string, object> metrics)
    {
        return new DiagnosticEvent(DiagnosticEventType.PerformanceMetrics, source, metrics);
    }

    /// <summary>
    /// Creates a strategy selected event.
    /// </summary>
    /// <param name="source">The source component.</param>
    /// <param name="strategyName">The name of the selected strategy.</param>
    /// <param name="fileSize">The file size that influenced the selection.</param>
    /// <param name="threshold">The threshold used for selection.</param>
    /// <returns>A diagnostic event.</returns>
    public static DiagnosticEvent StrategySelected(string source, string strategyName, long fileSize, long threshold)
    {
        var data = new Dictionary<string, object>
        {
            ["StrategyName"] = strategyName,
            ["FileSize"] = fileSize,
            ["Threshold"] = threshold
        };

        return new DiagnosticEvent(DiagnosticEventType.StrategySelected, source, data);
    }

    /// <summary>
    /// Creates a monitoring started event.
    /// </summary>
    /// <param name="source">The source component.</param>
    /// <param name="filePath">The file path being monitored.</param>
    /// <param name="lineCount">The number of lines to read on changes.</param>
    /// <returns>A diagnostic event.</returns>
    public static DiagnosticEvent MonitoringStarted(string source, string filePath, int lineCount)
    {
        var data = new Dictionary<string, object>
        {
            ["FilePath"] = filePath,
            ["LineCount"] = lineCount
        };

        return new DiagnosticEvent(DiagnosticEventType.MonitoringStarted, source, data);
    }

    /// <summary>
    /// Creates a monitoring stopped event.
    /// </summary>
    /// <param name="source">The source component.</param>
    /// <param name="filePath">The file path that was being monitored.</param>
    /// <returns>A diagnostic event.</returns>
    public static DiagnosticEvent MonitoringStopped(string source, string filePath)
    {
        var data = new Dictionary<string, object>
        {
            ["FilePath"] = filePath
        };

        return new DiagnosticEvent(DiagnosticEventType.MonitoringStopped, source, data);
    }

    /// <summary>
    /// Creates a file change detected event.
    /// </summary>
    /// <param name="source">The source component.</param>
    /// <param name="filePath">The file path that changed.</param>
    /// <param name="changeType">The type of change that occurred.</param>
    /// <param name="linesRead">The number of lines read after the change.</param>
    /// <returns>A diagnostic event.</returns>
    public static DiagnosticEvent FileChangeDetected(string source, string filePath, string changeType, int linesRead)
    {
        var data = new Dictionary<string, object>
        {
            ["FilePath"] = filePath,
            ["ChangeType"] = changeType,
            ["LinesRead"] = linesRead
        };

        return new DiagnosticEvent(DiagnosticEventType.FileChangeDetected, source, data);
    }
} 