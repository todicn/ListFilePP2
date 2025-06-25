namespace ListFile.Core.Interfaces;

/// <summary>
/// Defines the contract for diagnostic observers that can receive diagnostic events.
/// </summary>
public interface IDiagnosticObserver
{
    /// <summary>
    /// Called when a diagnostic event occurs.
    /// </summary>
    /// <param name="diagnosticEvent">The diagnostic event that occurred.</param>
    void OnDiagnosticEvent(IDiagnosticEvent diagnosticEvent);
}

/// <summary>
/// Defines the contract for diagnostic events.
/// </summary>
public interface IDiagnosticEvent
{
    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Gets the type of diagnostic event.
    /// </summary>
    DiagnosticEventType EventType { get; }

    /// <summary>
    /// Gets the source component that generated the event.
    /// </summary>
    string Source { get; }

    /// <summary>
    /// Gets additional event data.
    /// </summary>
    IDictionary<string, object> Data { get; }
}

/// <summary>
/// Represents the type of diagnostic event.
/// </summary>
public enum DiagnosticEventType
{
    /// <summary>
    /// File operation started.
    /// </summary>
    FileOperationStarted,

    /// <summary>
    /// File operation completed successfully.
    /// </summary>
    FileOperationCompleted,

    /// <summary>
    /// File operation failed.
    /// </summary>
    FileOperationFailed,

    /// <summary>
    /// Performance metrics collected.
    /// </summary>
    PerformanceMetrics,

    /// <summary>
    /// File monitoring started.
    /// </summary>
    MonitoringStarted,

    /// <summary>
    /// File monitoring stopped.
    /// </summary>
    MonitoringStopped,

    /// <summary>
    /// File change detected.
    /// </summary>
    FileChangeDetected,

    /// <summary>
    /// Strategy selection occurred.
    /// </summary>
    StrategySelected,

    /// <summary>
    /// Error occurred during operation.
    /// </summary>
    Error
} 