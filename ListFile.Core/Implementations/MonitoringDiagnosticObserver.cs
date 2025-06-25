using ListFile.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ListFile.Core.Implementations;

/// <summary>
/// Diagnostic observer that tracks file monitoring events and statistics.
/// </summary>
public class MonitoringDiagnosticObserver : IDiagnosticObserver
{
    private readonly ILogger<MonitoringDiagnosticObserver>? logger;
    private readonly Dictionary<string, MonitoringStatistics> monitoringStats;
    private readonly object lockObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitoringDiagnosticObserver"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for monitoring events.</param>
    public MonitoringDiagnosticObserver(ILogger<MonitoringDiagnosticObserver>? logger = null)
    {
        this.logger = logger;
        monitoringStats = new Dictionary<string, MonitoringStatistics>();
    }

    /// <inheritdoc />
    public void OnDiagnosticEvent(IDiagnosticEvent diagnosticEvent)
    {
        switch (diagnosticEvent.EventType)
        {
            case DiagnosticEventType.MonitoringStarted:
                HandleMonitoringStarted(diagnosticEvent);
                break;
            case DiagnosticEventType.MonitoringStopped:
                HandleMonitoringStopped(diagnosticEvent);
                break;
            case DiagnosticEventType.FileChangeDetected:
                HandleFileChangeDetected(diagnosticEvent);
                break;
            case DiagnosticEventType.Error:
                HandleError(diagnosticEvent);
                break;
        }
    }

    private void HandleMonitoringStarted(IDiagnosticEvent diagnosticEvent)
    {
        var filePath = diagnosticEvent.Data.TryGetValue("FilePath", out var fp) ? fp?.ToString() ?? "Unknown" : "Unknown";
        var lineCount = diagnosticEvent.Data.TryGetValue("LineCount", out var lc) ? lc : null;

        lock (lockObject)
        {
            monitoringStats[filePath] = new MonitoringStatistics
            {
                FilePath = filePath,
                StartTime = diagnosticEvent.Timestamp,
                LineCount = Convert.ToInt32(lineCount ?? 0),
                ChangeCount = 0
            };
        }

        logger?.LogInformation("File monitoring started for {FilePath} (reading {LineCount} lines on changes)", 
            Path.GetFileName(filePath), lineCount);
    }

    private void HandleMonitoringStopped(IDiagnosticEvent diagnosticEvent)
    {
        var filePath = diagnosticEvent.Data.TryGetValue("FilePath", out var fp) ? fp?.ToString() ?? "Unknown" : "Unknown";

        lock (lockObject)
        {
            if (monitoringStats.TryGetValue(filePath, out var stats))
            {
                stats.EndTime = diagnosticEvent.Timestamp;
                var duration = stats.EndTime.Value - stats.StartTime;

                logger?.LogInformation("File monitoring stopped for {FilePath}. Duration: {Duration}, Changes detected: {ChangeCount}", 
                    Path.GetFileName(filePath), duration, stats.ChangeCount);

                monitoringStats.Remove(filePath);
            }
        }
    }

    private void HandleFileChangeDetected(IDiagnosticEvent diagnosticEvent)
    {
        var filePath = diagnosticEvent.Data.TryGetValue("FilePath", out var fp) ? fp?.ToString() ?? "Unknown" : "Unknown";
        var changeType = diagnosticEvent.Data.TryGetValue("ChangeType", out var ct) ? ct?.ToString() ?? "Unknown" : "Unknown";
        var linesRead = diagnosticEvent.Data.TryGetValue("LinesRead", out var lr) ? lr : null;

        lock (lockObject)
        {
            if (monitoringStats.TryGetValue(filePath, out var stats))
            {
                stats.ChangeCount++;
                stats.LastChangeTime = diagnosticEvent.Timestamp;
            }
        }

        logger?.LogDebug("File change detected: {ChangeType} for {FilePath}, read {LinesRead} lines", 
            changeType, Path.GetFileName(filePath), linesRead);
    }

    private void HandleError(IDiagnosticEvent diagnosticEvent)
    {
        var message = diagnosticEvent.Data.TryGetValue("Message", out var msg) ? msg?.ToString() ?? "Unknown error" : "Unknown error";
        var exception = diagnosticEvent.Data.TryGetValue("Exception", out var ex) ? ex?.ToString() ?? "Unknown" : "Unknown";

        logger?.LogError("Monitoring error from {Source}: {Exception} - {Message}", 
            diagnosticEvent.Source, exception, message);
    }

    /// <summary>
    /// Gets the current monitoring statistics for all monitored files.
    /// </summary>
    /// <returns>A copy of the current monitoring statistics.</returns>
    public IReadOnlyDictionary<string, MonitoringStatistics> GetMonitoringStatistics()
    {
        lock (lockObject)
        {
            return new Dictionary<string, MonitoringStatistics>(monitoringStats);
        }
    }

    /// <summary>
    /// Represents monitoring statistics for a file.
    /// </summary>
    public class MonitoringStatistics
    {
        /// <summary>
        /// Gets or sets the file path being monitored.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the time monitoring started.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the time monitoring ended (if stopped).
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Gets or sets the number of lines being read on changes.
        /// </summary>
        public int LineCount { get; set; }

        /// <summary>
        /// Gets or sets the number of changes detected.
        /// </summary>
        public int ChangeCount { get; set; }

        /// <summary>
        /// Gets or sets the time of the last change detected.
        /// </summary>
        public DateTime? LastChangeTime { get; set; }

        /// <summary>
        /// Gets the duration of monitoring (if ended) or current duration.
        /// </summary>
        public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
    }
} 