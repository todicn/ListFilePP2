using ListFile.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ListFile.Core.Implementations;

/// <summary>
/// Diagnostic observer that tracks and logs performance metrics.
/// </summary>
public class PerformanceDiagnosticObserver : IDiagnosticObserver
{
    private readonly ILogger<PerformanceDiagnosticObserver>? logger;
    private readonly bool enableConsoleOutput;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceDiagnosticObserver"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for performance events.</param>
    /// <param name="enableConsoleOutput">Whether to output performance metrics to console.</param>
    public PerformanceDiagnosticObserver(ILogger<PerformanceDiagnosticObserver>? logger = null, bool enableConsoleOutput = true)
    {
        this.logger = logger;
        this.enableConsoleOutput = enableConsoleOutput;
    }

    /// <inheritdoc />
    public void OnDiagnosticEvent(IDiagnosticEvent diagnosticEvent)
    {
        switch (diagnosticEvent.EventType)
        {
            case DiagnosticEventType.FileOperationCompleted:
                HandleFileOperationCompleted(diagnosticEvent);
                break;
            case DiagnosticEventType.PerformanceMetrics:
                HandlePerformanceMetrics(diagnosticEvent);
                break;
            case DiagnosticEventType.StrategySelected:
                HandleStrategySelected(diagnosticEvent);
                break;
        }
    }

    private void HandleFileOperationCompleted(IDiagnosticEvent diagnosticEvent)
    {
        var filePath = diagnosticEvent.Data.TryGetValue("FilePath", out var fp) ? fp?.ToString() ?? "Unknown" : "Unknown";
        var operation = diagnosticEvent.Data.TryGetValue("Operation", out var op) ? op?.ToString() ?? "Unknown" : "Unknown";
        var elapsedMs = diagnosticEvent.Data.TryGetValue("ElapsedMilliseconds", out var em) ? em : null;
        var linesRead = diagnosticEvent.Data.TryGetValue("LinesRead", out var lr) ? lr : null;
        var fileSize = diagnosticEvent.Data.TryGetValue("FileSize", out var fs) ? fs : null;

        var message = $"File reading completed in {elapsedMs}ms";
        
        if (enableConsoleOutput)
        {
            Console.WriteLine(message);
        }

        if (logger != null)
        {
            using var scope = logger.BeginScope(new Dictionary<string, object>
            {
                ["FilePath"] = filePath,
                ["Operation"] = operation,
                ["ElapsedMilliseconds"] = elapsedMs ?? 0,
                ["LinesRead"] = linesRead ?? 0,
                ["FileSize"] = fileSize ?? 0
            });

            logger.LogInformation("File operation completed: {Operation} on {FilePath} in {ElapsedMs}ms", 
                operation, Path.GetFileName(filePath), elapsedMs);
        }
    }

    private void HandlePerformanceMetrics(IDiagnosticEvent diagnosticEvent)
    {
        logger?.LogInformation("Performance metrics collected from {Source}: {Metrics}", 
            diagnosticEvent.Source, 
            string.Join(", ", diagnosticEvent.Data.Select(kvp => $"{kvp.Key}={kvp.Value}")));
    }

    private void HandleStrategySelected(IDiagnosticEvent diagnosticEvent)
    {
        var strategyName = diagnosticEvent.Data.TryGetValue("StrategyName", out var sn) ? sn?.ToString() ?? "Unknown" : "Unknown";
        var fileSize = diagnosticEvent.Data.TryGetValue("FileSize", out var fs) ? fs : null;
        var threshold = diagnosticEvent.Data.TryGetValue("Threshold", out var th) ? th : null;

        logger?.LogDebug("Strategy selected: {StrategyName} for file size {FileSize} bytes (threshold: {Threshold})", 
            strategyName, fileSize, threshold);
    }
} 